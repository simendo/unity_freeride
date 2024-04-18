import cv2
import tensorflow as tf
import json
from ultralytics import YOLO
from itertools import chain

ultralytics_model = YOLO('yolov8n.pt')
#pose_model = YOLO('yolov8n-pose.pt')
custom_model = YOLO('/Users/simendomaas/Dokumenter_lokal/Valid/best.pt')

# Load video and get frame rate
cap = cv2.VideoCapture('/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/DSCF1414.MOV')
fps = cap.get(cv2.CAP_PROP_FPS)

# Predict with the model
results = custom_model('/Users/simendomaas/Documents/Skolearbeid/Masteroppgave/Media/DSCF1414.MOV', save = True)

bbox_data_to_export = []

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
        


final_timestamp = {"final_timestamp": timestamp}
bbox_data_to_export.append(final_timestamp)

with open('bounding_boxes.json', 'w') as f:
    json.dump(bbox_data_to_export, f)



