# This script uses a YOLO model to process a video file, detect objects in each frame, and export the bounding box data 
# to a JSON file. It also includes functionality to interpolate and extrapolate bounding box coordinates to generate 
# a smoother and more continuous sequence of detections across frames. The script calculates average changes between 
# detections to perform extrapolation and ensures the final detections are sorted by timestamp. The resulting data, 
# including both detected and interpolated bounding boxes, is saved to a JSON file.


import json
import numpy as np
import cv2
from ultralytics import YOLO


model = YOLO('path_to_model.pt')

videopath = 'path_to_video.mp4'

# Load video and get frame rate
cap = cv2.VideoCapture(videopath)
fps = cap.get(cv2.CAP_PROP_FPS)

# Predict with the model
results = model(videopath, save = True)

bbox_data_to_export = []
pose_data_to_export = []

for frame_idx, result in enumerate(results):
    
    timestamp = frame_idx / fps

    for box in result.boxes:
        flattened_coordinates = [coord.item() for sublist in box.xyxy for coord in sublist]

        bbox_data = {
            "timestamp": timestamp,  
            "coordinates": flattened_coordinates,  
            "class_id": box.cls.tolist(),
            "class_name": [result.names[c] for c in box.cls.tolist()]
        }
        bbox_data_to_export.append(bbox_data)

final_time = timestamp
final_timestamp = {"final_timestamp": timestamp}
#bbox_data_to_export.append(final_timestamp)

# Export to JSON
with open('path_to_bboxdata.json', 'w') as f:
    json.dump(bbox_data_to_export, f)

with open('path_to_bboxdata.json', 'r') as file:
    detections = json.load(file)


#This next part is used to add more data to the original file of detections based on interpolation between previous data points

# Function to interpolate between two detections
def interpolate_detections(det1, det2, num_frames):
    coords1 = np.array(det1['coordinates'])
    coords2 = np.array(det2['coordinates'])
    timestamps = np.linspace(det1['timestamp'], det2['timestamp'], num=num_frames+2)  # Include start and end
    interpolated_coords = np.linspace(coords1, coords2, num=num_frames+2, axis=0)
    rounded_timestamps = [round(ts, 2) for ts in timestamps[1:-1]]  # Exclude the start and end since they are already known
    return rounded_timestamps, interpolated_coords[1:-1]

#Function used in the extrapolation of detections
def calculate_average_changes(detections):
    # Calculate differences between consecutive detections
    diffs = [np.array(detections[i+1]['coordinates']) - np.array(detections[i]['coordinates']) 
             for i in range(len(detections) - 1)]
    # Average these differences
    average_diffs = np.mean(diffs, axis=0)
    avg_x = (average_diffs[0] + average_diffs[2]) / 2
    avg_y = (average_diffs[1] + average_diffs[3]) / 2
    adjusted_average_diffs = np.array([avg_x, avg_y, avg_x, avg_y])

    return adjusted_average_diffs

def extrapolate_detections(last_detection, average_changes, final_time, fps):
    extrapolated_detections = []
    current_coords = np.array(last_detection['coordinates'])
    current_time = last_detection['timestamp']

    while current_time < final_time:
        current_time += 1 / fps
        current_time = round(current_time, 2)
        current_coords += average_changes
        extrapolated_detections.append({
            "timestamp": current_time,
            "coordinates": current_coords.tolist(),
            "class_id": last_detection['class_id'],
            "class_name": last_detection['class_name']
        })

    return extrapolated_detections

filtered_detections = [det for det in detections if 'coordinates' in det] #Kan fjernes
interpolated_detections = []
for i in range(len(filtered_detections) - 1):
    det1 = filtered_detections[i]
    det2 = filtered_detections[i + 1]
    time_diff = int((det2['timestamp'] - det1['timestamp']) * fps) - 1
    if time_diff > 1:
        timestamps, coords = interpolate_detections(det1, det2, time_diff)
        for ts, coord in zip(timestamps, coords):
            interpolated_detections.append({
                "timestamp": ts,
                "coordinates": coord.tolist(),
                "class_id": det1['class_id'],
                "class_name": det1['class_name']
            })

# Combine detections and interpolated detections
combined_detections = filtered_detections + interpolated_detections

# Sort combined detections by timestamp
combined_detections.sort(key=lambda x: x['timestamp'])

last_known_detection = combined_detections[-1]
time_steps = np.arange(last_known_detection['timestamp'], final_time, 1/fps)  
average_changes = calculate_average_changes(combined_detections[-15:])
print("average_changes: ", average_changes)

final_detections = extrapolate_detections(combined_detections[-1], average_changes, final_time, fps)

combined_detections += final_detections
combined_detections.sort(key=lambda x: x['timestamp'])

for detection in combined_detections:
    detection['timestamp'] = round(detection['timestamp'], 2)

with open('bounding_boxes_interpolated.json', 'w') as f:
    json.dump(combined_detections, f)

