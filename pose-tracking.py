import cv2
import mediapipe as mp

mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

# Action detection functions
def detect_step(landmarks):
    right_knee_y = landmarks[mp_pose.PoseLandmark.RIGHT_KNEE].y
    right_hip_y = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].y
    return right_knee_y < right_hip_y

def detect_kick(landmarks):
    right_ankle_y = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE].y
    right_knee_y = landmarks[mp_pose.PoseLandmark.RIGHT_KNEE].y
    return right_ankle_y < right_knee_y  # Ankle raised above knee

def detect_punch(landmarks):
    right_wrist_x = landmarks[mp_pose.PoseLandmark.RIGHT_WRIST].x
    right_shoulder_x = landmarks[mp_pose.PoseLandmark.RIGHT_SHOULDER].x
    return right_wrist_x < right_shoulder_x  # Fist moved forward (to the left in image)

def detect_jump(landmarks):
    left_ankle_y = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE].y
    right_ankle_y = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE].y
    left_hip_y = landmarks[mp_pose.PoseLandmark.LEFT_HIP].y
    right_hip_y = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].y
    return (left_ankle_y < left_hip_y) and (right_ankle_y < right_hip_y)

# Main camera logic
cap = cv2.VideoCapture(0)

with mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5) as pose:
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            break

        # Convert image to RGB
        image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        image.flags.writeable = False

        # Process the image and detect the pose
        results = pose.process(image)

        # Convert back to BGR for display
        image.flags.writeable = True
        image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

        if results.pose_landmarks:
            landmarks = results.pose_landmarks.landmark

            mp_drawing.draw_landmarks(
                image, results.pose_landmarks, mp_pose.POSE_CONNECTIONS)

            # Detect actions
            if detect_step(landmarks):
                cv2.putText(image, "Step Forward!", (50, 100), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                print("Step forward!")

            if detect_kick(landmarks):
                cv2.putText(image, "Kick!", (50, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                print("Kick!")

            if detect_punch(landmarks):
                cv2.putText(image, "Punch!", (50, 200), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 2)
                print("Punch!")

            if detect_jump(landmarks):
                cv2.putText(image, "Jump!", (50, 250), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
                print("Jump!")

        # Display image
        cv2.imshow('MediaPipe Pose', image)

        if cv2.waitKey(10) & 0xFF == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
