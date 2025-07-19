# Import the OpenCV library for image processing
import cv2
# Import the MediaPipe library for pose detection
import mediapipe as mp

# Initialize the pose estimation model from MediaPipe
mp_pose = mp.solutions.pose
pose = mp_pose.Pose()

# Start capturing video from the default webcam (index 0)
cap = cv2.VideoCapture(0)

# Loop continuously while the webcam is open
while cap.isOpened():
    # Read a frame from the webcam
    ret, frame = cap.read()
    # If the frame was not captured successfully, skip this iteration
    if not ret:
        continue

    # Convert the frame from BGR (OpenCV default) to RGB (required by MediaPipe)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    # Process the RGB image using the pose model to detect human landmarks
    result = pose.process(rgb)

    # If pose landmarks are detected in the frame
    if result.pose_landmarks:
        # Print all the pose landmarks to the console
        print(result.pose_landmarks)

        # Get height and width of the frame for coordinate scaling
        h, w, _ = frame.shape
        # Loop through each detected landmark in the pose
        for landmark in result.pose_landmarks.landmark:
            # Convert normalized landmark coordinates to pixel coordinates
            cx, cy = int(landmark.x * w), int(landmark.y * h)
            # Draw a small blue circle at each landmark location
            cv2.circle(frame, (cx, cy), 5, (255, 0, 0), -1)

        # Get the landmark data for the right wrist
        right_wrist = result.pose_landmarks.landmark[mp_pose.PoseLandmark.RIGHT_WRIST]
        # Get the landmark data for the right shoulder
        right_shoulder = result.pose_landmarks.landmark[mp_pose.PoseLandmark.RIGHT_SHOULDER]

        # Convert normalized coordinates of right wrist to pixels
        wrist_x, wrist_y = int(right_wrist.x * w), int(right_wrist.y * h)
        # Convert normalized coordinates of right shoulder to pixels
        shoulder_x, shoulder_y = int(right_shoulder.x * w), int(right_shoulder.y * h)

        # Draw a green circle at the right wrist
        cv2.circle(frame, (wrist_x, wrist_y), 8, (0, 255, 0), -1)
        # Draw a red circle at the right shoulder
        cv2.circle(frame, (shoulder_x, shoulder_y), 8, (0, 0, 255), -1)

        # If the y-coordinate of the wrist is above the shoulder (hand raised)
        if right_wrist.y < right_shoulder.y:
            # Print message indicating hand is raised
            print("Hand raised!")

    # Display the frame with annotations in a window named "Pose Tracking"
    cv2.imshow("Pose Tracking", frame)
    # Wait for a key press; break the loop if 'q' is pressed
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release the webcam resource
cap.release()
# Close all OpenCV windows
cv2.destroyAllWindows()
