import cv2
import tensorflow as tf
import json
from ultralytics import YOLO
from itertools import chain

#model = YOLO('yolov8n.pt')
model = YOLO('yolov8n-pose.pt')

# Load video and get frame rate
cap = cv2.VideoCapture('/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/DSCF1414.MOV')
fps = cap.get(cv2.CAP_PROP_FPS)

# Predict with the model
results = model('/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/DSCF1414.MOV', save = True)

bbox_data_to_export = []
pose_data_to_export = []

for frame_idx, result in enumerate(results):
    
    timestamp = frame_idx / fps

    for box in result.boxes:
        flattened_coordinates = [coord.item() for sublist in box.xyxy for coord in sublist]

        bbox_data = {
            "timestamp": timestamp,  
            "coordinates": flattened_coordinates,  
            #"confidence": box.conf.tolist(),
            "class_id": box.cls.tolist(),
            "class_name": [result.names[c] for c in box.cls.tolist()]
        }
        bbox_data_to_export.append(bbox_data)
        
    for kp in result.keypoints:
        if kp.xy is not None:
            keypoints_list = kp.xy.numpy().tolist()

            pose_data = {
                "timestamp": timestamp,
                "keypoints": keypoints_list,
            }
            pose_data_to_export.append(pose_data)

    #gir en lang liste for keypoints: 
    # for kp in result.keypoints:
    #    if kp.xy is not None:
    #        keypoints_list = [coord for sublist in kp.xy.tolist() for item in sublist for coord in item]
    #        
    #        pose_data = {
    #            "timestamp": timestamp,
    #            "keypoints": keypoints_list,
    #        }
    #        pose_data_to_export.append(pose_data)


#final_timestamp = {"final_timestamp": timestamp}
#bbox_data_to_export.append(final_timestamp)
# Export to JSON
#with open('bounding_boxes.json', 'w') as f:
#    json.dump(bbox_data_to_export, f)

with open('pose_data_flattened3.json', 'w') as f:
    json.dump(pose_data_to_export, f)


