import cv2
import mediapipe as mp
import numpy as np
import socket
import json

# UDP socket setup
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_address = ('127.0.0.1', 5052)  # Unity's receiving port

# Initialize MediaPipe Hand model
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(static_image_mode=False, max_num_hands=2, min_detection_confidence=0.5)
mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles

# Initialize webcam
cap = cv2.VideoCapture(0)

def landmark_to_dict(landmark):
    return {"x": landmark.x, "y": landmark.y, "z": landmark.z}

while cap.isOpened():
    success, image = cap.read()
    if not success:
        print("Can't find the webcam.")
        continue

    # Convert BGR image to RGB
    image = cv2.cvtColor(cv2.flip(image, 1), cv2.COLOR_BGR2RGB)
    
    # Perform hand detection
    results = hands.process(image)
    
    # Convert the image back to BGR
    image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

    hands_data = {"left": None, "right": None}
    
    if results.multi_hand_landmarks:
        for hand_landmarks, handedness in zip(results.multi_hand_landmarks, results.multi_handedness):
            # Draw hand landmarks
            mp_drawing.draw_landmarks(
                image,
                hand_landmarks,
                mp_hands.HAND_CONNECTIONS,
                mp_drawing_styles.get_default_hand_landmarks_style(),
                mp_drawing_styles.get_default_hand_connections_style())
            
            # Convert landmark data to list of dictionaries
            landmarks_list = [landmark_to_dict(lm) for lm in hand_landmarks.landmark]
            
            # Determine left/right hand
            hand_side = "left" if handedness.classification[0].label == "Left" else "right"
            
            # Assign to the appropriate hand in the data dictionary
            hands_data[hand_side] = {"landmarks": landmarks_list}
            
            # Draw landmark indices
            for i, landmark in enumerate(hand_landmarks.landmark):
                x = int(landmark.x * image.shape[1])
                y = int(landmark.y * image.shape[0])
                cv2.putText(image, str(i), (x, y), cv2.FONT_HERSHEY_SIMPLEX, 0.3, (255, 0, 0), 1)

    # Send data to Unity
    json_data = json.dumps(hands_data)
    sock.sendto(json_data.encode(), unity_address)

    # Display hand detection status
    cv2.putText(image, f"Left hand: {'Detected' if hands_data['left'] else 'Not detected'}", 
                (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
    cv2.putText(image, f"Right hand: {'Detected' if hands_data['right'] else 'Not detected'}", 
                (10, 70), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

    # Show results
    cv2.imshow('Hand Tracking', image)
    
    if cv2.waitKey(5) & 0xFF == 27:  # Press ESC to exit
        break

cap.release()
cv2.destroyAllWindows()
sock.close()