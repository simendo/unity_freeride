from operator import gt
import cv2
import numpy as np
from ultralytics import YOLO
import os
import yaml

# Initialize models
ultralytics_model = YOLO('yolov8n.pt')
custom_model = YOLO('/Users/simendomaas/Dokumenter_lokal/Valid/best.pt')

image_dir = '/Users/simendomaas/Dokumenter_lokal/Valid/Ground Truth/valid/images'
label_dir = '/Users/simendomaas/Dokumenter_lokal/Valid/Ground Truth/valid/labels'
dataset_config = '/Users/simendomaas/Dokumenter_lokal/Valid/Ground Truth/data.yaml'

# Load dataset config to map class names
with open(dataset_config, 'r') as file:
    dataset = yaml.safe_load(file)  # Load dataset config
    if dataset is None:
        print("Failed to load dataset")  # Debugging


def compare_boxes(gt_box, pred_box):
    # Extract bounding box coordinates
    
    x1_pred, y1_pred, x2_pred, y2_pred = pred_box

    x1_pred=x1_pred/3840
    x2_pred=x2_pred/3840
    y1_pred=y1_pred/2160
    y2_pred=y2_pred/2160

    # Calculate Intersection over Union (IoU)
    xi1 = max(gt_box[0], x1_pred)
    yi1 = max(gt_box[1], y1_pred)
    xi2 = min(gt_box[2], x2_pred-x1_pred)
    yi2 = min(gt_box[3], y2_pred-y1_pred)
    inter_area = max(xi2 - xi1, 0) * max(yi2 - yi1, 0)

    #print('gt sizes:')
    #print(gt_box[0], gt_box[1], gt_box[2], gt_box[3])
    #print('pred sizes:')
    #print(x1_pred, y1_pred, x2_pred - x1_pred, y2_pred - y1_pred)
    #print('\n')

    gt_area = (gt_box[2] - gt_box[0]) * (gt_box[3] - gt_box[1])
    pred_area = (x2_pred - x1_pred) * (y2_pred - y1_pred)
    union_area = gt_area + pred_area - inter_area

    iou = inter_area / union_area if union_area != 0 else 0

    # Determine if the IoU exceeds threshold
    if iou > 0.3:
        return True
    #else: 
    #    print('iou:') 
    #    print(iou)
    #    print('\n')
    return False

def display_bboxes(gt_box, pred_box, image): 
    # Extract bounding box coordinates
    x1_gt, y1_gt, x2_gt, y2_gt = gt_box
    x1_pred, y1_pred, x2_pred, y2_pred = pred_box
    # Draw bounding boxes on the image
    color_gt = (0, 255, 0)  # Green color for ground truth
    color_pred = (0, 0, 255)  # Red color for prediction
    thickness = 2

    x1_gt_scaled = x1_gt * 3840
    y1_gt_scaled = y1_gt * 2160
    x2_gt_scaled = x2_gt * 3840
    y2_gt_scaled = y2_gt * 2160

    # Draw ground truth bounding box
    cv2.rectangle(image, (int(x1_gt_scaled-(x2_gt_scaled/2)), int(y1_gt_scaled-(y2_gt_scaled/2))), (int(x2_gt_scaled+(x1_gt_scaled)), int(y2_gt_scaled+(y1_gt_scaled))), color_gt, thickness)

    # Draw predicted bounding box
    cv2.rectangle(image, (int(x1_pred), int(y1_pred)), (int(x2_pred), int(y2_pred)), color_pred, thickness)


    # Resize the image to fit the screen
    height, width = image.shape[:2]
    max_height = 800
    max_width = 1200
    if height > max_height or width > max_width:
        scale = min(max_height / height, max_width / width)
        resized_image = cv2.resize(image, None, fx=scale, fy=scale)
    else:
        resized_image = image

    # Display the image with bounding boxes for this iteration
    cv2.imshow(f"Iteration ", resized_image)
    cv2.waitKey(0)  # Wait for key press to close the image window
    cv2.destroyAllWindows()  # Close all OpenCV windows

    print('scaled gt values are: ')
    print(int(x1_gt*3840), int(y1_gt*2160)), (int(x2_gt*3840), int(y2_gt*2160))
    print('they should be: ')
    print(int(x1_pred), int(y1_pred)), (int(x2_pred), int(y2_pred))
    print('\n')

# Iterate over images and labels
for filename in os.listdir(image_dir):
    if filename.endswith('.jpg'):
        # Load image
        image_file = os.path.join(image_dir, filename)
        image = cv2.imread(image_file)
        if image is None:
            print("Failed to load image:", image_file)  # Debugging
            continue

        # Load label
        label_file = os.path.join(label_dir, os.path.splitext(filename)[0] + '.txt')
        if not os.path.exists(label_file):
            print("Label file not found for:", image_file)  # Debugging
            continue

        with open(label_file, 'r') as f:
            label_data = f.read().strip().split()
            gt_box = [float(coord) for coord in label_data[1:]]

        # Make predictions with ultralytics model
        results_ultralytics = ultralytics_model(image)

        # Make predictions with custom model
        results_custom = custom_model(image)

        # Extract bounding boxes, classes, names, and confidences
        if results_ultralytics[0].boxes.cls.tolist() == 0:
            pred_boxes_ultralytics = results_custom[0].boxes.xyxy.tolist()

        confidence_threshold = 0.25

        for uresult in results_ultralytics:
            boxes = uresult.boxes.xyxy.tolist()
            classes = uresult.boxes.cls.tolist()    
            names = uresult.names
            confidences = uresult.boxes.conf.tolist()
            #print(boxes)
            for box, cls, conf in zip(boxes, classes, confidences):
                if conf < confidence_threshold:
                    continue
                if names[int(cls)] == 'person':
                    x1, y1, x2, y2 = box
                    if len(gt_box) == 4:
                        #if (image_file == 'DSCF1414_MOV-0200_jpg.rf.10a0c316b45044469e733277b373eff6'):
                        display_bboxes(gt_box, box, image)
                        if compare_boxes(gt_box, box):
                            print("NIIICE!!!!!!!!")
                        #else:
                            #print('NOT SO NICE...') 
                            #print(x1/3840, y1/2160, x2/3840-x1/3840, y2/2160-y1/2160)
                            #print(gt_box)
                            #print('\n')
                    
                    #x1=x1/640
                    #x2=x2/640
                    #y1=y1/640
                    #y2=y2/640
                    #print(cls,names[int(cls)],x1,y1,x2,y2)
                    #confidence = conf
                    #detected_class = cls
                    #name = names[int(cls)]

