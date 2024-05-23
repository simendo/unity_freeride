
# This script compares the performance of two YOLO models (one pre-trained and one custom-trained) on object detection tasks.
# It evaluates the models based on their predictions against ground truth labels, calculates performance metrics,
# and visualizes bounding boxes for both ground truth and predictions.

import cv2
from ultralytics import YOLO
import os
import yaml

# Initialize models
ultralytics_model = YOLO('yolov8n.pt')
#ultralytics_model = YOLO('yolov8m-pose.pt') #used to evaluate pose model
custom_model = YOLO('/Users/simendomaas/Dokumenter_lokal/Valid/best.pt')

image_dir = '/Users/simendomaas/Dokumenter_lokal/Tests/GroundTruth_Light/valid/images'
label_dir = '/Users/simendomaas/Dokumenter_lokal/Tests/GroundTruth_Light/valid/labels'
dataset_config = '/Users/simendomaas/Dokumenter_lokal/Tests/GroundTruth_Light/data.yaml'
#image_dir = '/Users/simendomaas/Dokumenter_lokal/Tests/GroundTruth_Dark/valid/images'
#label_dir = '/Users/simendomaas/Dokumenter_lokal/Tests/GroundTruth_Dark/valid/labels'
#dataset_config = '/Users/simendomaas/Dokumenter_lokal/Tests/GroundTruth_Dark/data.yaml'

with open(dataset_config, 'r') as file:
    dataset = yaml.safe_load(file) 
    if dataset is None:
        print("Failed to load dataset")  


def compare_boxes(gt_box, pred_box):
    # Extract bounding box coordinates
    x1_gt, y1_gt, x2_gt, y2_gt = gt_box
    x1_pred, y1_pred, x2_pred, y2_pred = pred_box

    x1_gt_scaled = x1_gt * 3840
    y1_gt_scaled = y1_gt * 2160
    x2_gt_scaled = x2_gt * 3840
    y2_gt_scaled = y2_gt * 2160

    # Calculate Intersection over Union (IoU)
    xi1 = max(x1_gt_scaled-(x2_gt_scaled/2), x1_pred)
    yi1 = max(y1_gt_scaled-(y2_gt_scaled/2), y1_pred)
    xi2 = min((x2_gt_scaled/2)+x1_gt_scaled, x2_pred)
    yi2 = min((y2_gt_scaled/2)+y1_gt_scaled, y2_pred)
    inter_area = max(xi2 - xi1, 0) * max(yi2 - yi1, 0)

    gt_area = x2_gt_scaled * y2_gt_scaled
    pred_area = (x2_pred - x1_pred) * (y2_pred - y1_pred)
    union_area = gt_area + pred_area - inter_area

    iou = inter_area / union_area if union_area != 0 else 0

    if iou > 0.6:
        return True
    #else: 
    #    print(iou)
    return False

def display_bboxes(gt_box, pred_box_pretrained, pred_box_custom, image): 
    # Extract bounding box coordinates
    x1_gt, y1_gt, x2_gt, y2_gt = gt_box
    x1_pred_pt, y1_pred_pt, x2_pred_pt, y2_pred_pt = pred_box_pretrained
    x1_pred_c, y1_pred_c, x2_pred_c, y2_pred_c = pred_box_custom
    # Draw bounding boxes on the image
    color_gt = (0, 255, 0)  # Green color for ground truth
    color_pt = (0, 0, 255)  # Red color for prediction from pretrained model
    color_c = (255, 0, 200) # Blue color for prediction from custom model
    thickness = 2

    x1_gt_scaled = x1_gt * 3840
    y1_gt_scaled = y1_gt * 2160
    x2_gt_scaled = x2_gt * 3840/2
    y2_gt_scaled = y2_gt * 2160/2

    # Draw ground truth bounding box
    cv2.rectangle(image, (int(x1_gt_scaled-x2_gt_scaled), int(y1_gt_scaled-y2_gt_scaled)), (int(x2_gt_scaled+x1_gt_scaled), int(y2_gt_scaled+y1_gt_scaled)), color_gt, thickness)
    cv2.putText(image, "Ground Truth", (int(x1_gt_scaled-x2_gt_scaled), int(y1_gt_scaled-y2_gt_scaled)-10), cv2.FONT_HERSHEY_SIMPLEX, 1.5, color_gt, 2)

    # Draw predicted bounding box for pretrained model
    if (pred_box_pretrained != 0,0,0,0):
        cv2.rectangle(image, (int(x1_pred_pt), int(y1_pred_pt)), (int(x2_pred_pt), int(y2_pred_pt)), color_pt, thickness)
        cv2.putText(image, "Pretrained Model Detection", (int(x1_pred_pt), int(y2_pred_pt)-10), cv2.FONT_HERSHEY_SIMPLEX, 1.5, color_pt, 2)

    # Draw predicted bounding box for custom model
    if (pred_box_custom != 0,0,0,0):
        cv2.rectangle(image, (int(x1_pred_c), int(y1_pred_c)), (int(x2_pred_c), int(y2_pred_c)), color_c, thickness)
        cv2.putText(image, "Custom Model Detection", (int(x1_pred_c), int(y2_pred_c)-10), cv2.FONT_HERSHEY_SIMPLEX, 1.5, color_c, 2)

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


custom_correct_predictions = 0
custom_total_predictions = 0
pretrained_correct_predictions = 0
pretrained_total_predictions = 0
pretrained_wrong_class = 0


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

        confidence_threshold = 0.25
        pretrained_detection = False
        custom_detection = False
        pretrained_bbox = 0,0,0,0
        custom_bbox = 0,0,0,0

        # Process ultralytics results
        for uresult in results_ultralytics:
            boxes = uresult.boxes.xyxy.tolist()
            classes = uresult.boxes.cls.tolist()
            for box, cls in zip(boxes, classes):
                if cls != 0:
                    pretrained_wrong_class += 1  
                x1, y1, x2, y2 = box
                if len(gt_box) == 4:
                    if compare_boxes(gt_box, box) & (cls == 0):
                        pretrained_correct_predictions += 1 
                        pretrained_detection = True
                        pretrained_bbox = box
                pretrained_total_predictions += 1  

        # Process custom model results
        for result in results_custom:
            boxes = result.boxes.xyxy.tolist()
            classes = result.boxes.cls.tolist()
            for box, cls in zip(boxes, classes):
                if cls == 1: #Does not evaluate detection of skis 
                    x1, y1, x2, y2 = box
                    if len(gt_box) == 4:
                        if compare_boxes(gt_box, box):
                            custom_correct_predictions += 1 
                            custom_detection = True
                            custom_bbox = box
                            #display_bboxes(gt_box, pretrained_bbox, custom_bbox, image)
                    custom_total_predictions += 1  


total_labels = len(os.listdir(label_dir))  # Total number of labels in the ground truth dataset

# Compute Precision, Recall, and F1 Score
def compute_metrics(correct_predictions, total_predictions, total_labels):
    precision = correct_predictions / total_predictions if total_predictions > 0 else 0
    recall = correct_predictions / total_labels if total_labels > 0 else 0
    f1_score = 2 * (precision * recall) / (precision + recall) if (precision + recall) > 0 else 0
    return precision, recall, f1_score

# Calculate metrics for pretrained and custom models
pretrained_precision, pretrained_recall, pretrained_f1 = compute_metrics(pretrained_correct_predictions, pretrained_total_predictions, total_labels)
custom_precision, custom_recall, custom_f1 = compute_metrics(custom_correct_predictions, custom_total_predictions, total_labels)

print(f"Total labels: {total_labels}\n")
# Print detailed performance metrics
print(f"Pretrained Model Performance:\n"
      f"Accuracy: {pretrained_correct_predictions / total_labels if total_labels > 0 else 0:.2f}\n"
      f"Precision: {pretrained_precision:.2f}\n"
      f"Recall: {pretrained_recall:.2f}\n"
      f"F1 Score: {pretrained_f1:.2f}\n"
      f"Total Predictions: {pretrained_total_predictions}\n"
      f"Correct Predictions: {pretrained_correct_predictions}\n"
      f"Detections of Incorrect Classes: {pretrained_wrong_class}\n")

print(f"Custom Model Performance:\n"
      f"Accuracy: {custom_correct_predictions / total_labels if total_labels > 0 else 0:.2f}\n"
      f"Precision: {custom_precision:.2f}\n"
      f"Recall: {custom_recall:.2f}\n"
      f"F1 Score: {custom_f1:.2f}\n"
      f"Total Predictions: {custom_total_predictions}\n"
      f"Correct Predictions: {custom_correct_predictions}")

