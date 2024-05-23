# This script uses a YOLO model to process a video file, detecting objects (and optionally, keypoints for pose estimation) 
# in each frame. It saves the results, including bounding box coordinates, class IDs, class names, and timestamps, to a 
# JSON file. Additionally, if a pose estimation model is used, it exports the keypoint data to a separate JSON file. 
# The script allows the user to choose between different YOLO models (custom-trained, pre-trained, or pose estimation) 
# and can adjust the confidence threshold for detections.

import json
import numpy as np
import cv2
from ultralytics import YOLO

posemodel = False

#Uncomment one of the models
model = YOLO('/Users/simendomaas/Dokumenter_lokal/Valid/best.pt') #custom model
#model = YOLO('yolov8m.pt') #pretrained model
#model = YOLO('yolov8m-pose.pt') #pose estimation model
#model.conf = 0.35

videopath = '/Users/simendomaas/Dokumenter_lokal/Media/Vågå/Videoklipp/angle1.mp4'
#videopath = '/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/DSCF1414.MOV'

# Load video and get frame rate
cap = cv2.VideoCapture(videopath)
fps = cap.get(cv2.CAP_PROP_FPS)

# Predict with the model
#results = model('/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/DSCF1414.MOV', save = True)
results = model(videopath, save = True)
#results = model(videopath, hide_labels=True, hide_conf=True, show=True, boxes=False, save=True)


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
    if posemodel:
        for kp in result.keypoints:
            if kp.xy is not None:
                keypoints_flattened = kp.xy.numpy().flatten().tolist()
                pose_data = {
                    "timestamp": timestamp,
                    "keypoints": keypoints_flattened
                }
                pose_data_to_export.append(pose_data)


#final_time = timestamp
#final_timestamp = {"final_timestamp": timestamp}
#bbox_data_to_export.append(final_timestamp)

# Export to JSON
with open('test_angle_bbox.json', 'w') as f:
        json.dump(bbox_data_to_export, f)   

if posemodel:
    with open('test_angle_pose.json', 'w') as f:
        json.dump(pose_data_to_export, f)



