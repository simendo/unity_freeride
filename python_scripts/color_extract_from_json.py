from PIL import Image
import cv2
import json
import numpy as np
import io
from colorthief import ColorThief

#This script takes in a json file and a video and finds the dominant color within the 
#bounding box area defined in the json. As the videos are recorded during night time
#and typically have black or dark grey background, these colors are filtered out
#such that the extracted color is more likely to represent the color of the LEDs 
#worn by the skier. 

with open('interpolated_bounding_boxes_angle1.json', 'r') as file:
    bbox_data = json.load(file)

cap = cv2.VideoCapture('/Users/simendomaas/Dokumenter_lokal/Media/Vågå/angle1_cut.mp4')
if not cap.isOpened():
    print("Error opening video file")
    exit()

fps = cap.get(cv2.CAP_PROP_FPS)
frame_count = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))

#Determine if the color is not black or grey.
def is_color_bright(color, black_threshold=50, grey_threshold=15):
    if max(color) < black_threshold:  #Close to black
        return False
    if max(color) - min(color) < grey_threshold:  # Close to grey
        return False
    return True

def get_dominant_color(image, palette_size=6):
    # Save image to a buffer
    is_success, im_buf_arr = cv2.imencode(".jpg", image)
    if not is_success:
        return None
    image_bytes = im_buf_arr.tobytes()
    byte_stream = io.BytesIO(image_bytes)

    # Pass the byte stream directly to ColorThief
    color_thief = ColorThief(byte_stream)
    palette = color_thief.get_palette(color_count=palette_size, quality=1)

    # Filter out dark or grey colors
    filtered_colors = [color for color in palette if is_color_bright(color)]

    return filtered_colors if filtered_colors else None  # Return None if all colors are filtered out

# Augment bounding box data with colors
updated_bbox_data = []

frame_idx = 0
while True:
    ret, frame = cap.read()
    if not ret:
        break

    frame_data = [d for d in bbox_data if ('timestamp' in d and int(d['timestamp'] * fps) == frame_idx) or 'final_timestamp' in d]
    for data in frame_data:
        if 'coordinates' in data: 
            x1, y1, x2, y2 = map(int, data['coordinates'])
            if y1 < y2 and x1 < x2:
                crop_img = frame[y1:y2, x1:x2]
                if crop_img.size > 0:
                    colors = get_dominant_color(crop_img)
                    if colors:
                        # Boost the first color from the filtered list
                        color = colors[0]
                        hsv_color = cv2.cvtColor(np.uint8([[color]]), cv2.COLOR_RGB2HSV)
                        hsv_color[0][0][2] = min(hsv_color[0][0][2] + 40, 255)
                        boosted_color = tuple(map(int, cv2.cvtColor(hsv_color, cv2.COLOR_HSV2RGB)[0][0]))

                        # Append the color to the bounding box data
                        data['color'] = boosted_color
            updated_bbox_data.append(data)
        elif 'final_timestamp' in data:
            final_entry = data  # Save the final_timestamp for later

    frame_idx += 1
    if frame_idx >= frame_count:
        break

cap.release()

# Save the updated bounding box data to a new JSON file
with open('bounding_boxes_with_colors.json', 'w') as f:
    json.dump(updated_bbox_data, f)

print("Updated bounding box data saved with color information.")