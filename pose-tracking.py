import cv2
import mediapipe as mp
import numpy as np
import time

mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

# Jump detection variables
prev_left_foot_y = None
prev_right_foot_y = None
foot_velocity_threshold = 0.01  # Minimum y-change per frame to count as jump
jump_cooldown = 0.5  # seconds
last_jump_time = 0
velocity_window = 3  # frames to average velocity over
left_foot_velocities = []
right_foot_velocities = []

# Punch detection state variables
left_punching = False
right_punching = False

# Action detection functions
def detect_step(landmarks):
    right_knee_y = landmarks[mp_pose.PoseLandmark.RIGHT_KNEE].y
    right_hip_y = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].y
    return right_knee_y < right_hip_y

# Kick detection state variables
last_kick_time = 0
kick_cooldown = 0.5  # seconds
kicking_state = False
normal_ankle_distance = None
KICK_DISTANCE_THRESHOLD = 1.5  # multiplier for normal distance to detect kick

def detect_kick(landmarks, current_time):
    global last_kick_time, kicking_state, normal_ankle_distance
    
    left_ankle = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE]
    right_ankle = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE]
    
    # Calculate current ankle distance using both horizontal and vertical components
    current_distance = np.sqrt(
        (left_ankle.x - right_ankle.x)**2 + 
        (left_ankle.y - right_ankle.y)**2
    )
    
    # Initialize or update normal ankle distance during standing
    if normal_ankle_distance is None or not kicking_state:
        if abs(left_ankle.y - right_ankle.y) < 0.1:  # Feet roughly level
            normal_ankle_distance = current_distance
    
    # Return false if we don't have a baseline yet
    if normal_ankle_distance is None:
        return False
    
    # Check if current distance is significantly larger than normal
    kick_detected = current_distance > (normal_ankle_distance * KICK_DISTANCE_THRESHOLD)
    
    # Handle cooldown and state
    if kick_detected and not kicking_state and (current_time - last_kick_time) > kick_cooldown:
        kicking_state = True
        last_kick_time = current_time
        
        # Get hip positions to determine kick direction
        left_hip_z = landmarks[mp_pose.PoseLandmark.LEFT_HIP].z
        right_hip_z = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].z
        
        # Determine kick direction based on which hip is closer to the camera
        if right_hip_z < left_hip_z:  # Right hip is closer (smaller z value)
            print("Left Kick!")  # Kicking towards the left
        else:
            print("Right Kick!")  # Kicking towards the right
        return True
    elif not kick_detected:
        kicking_state = False
    
    return False

def detect_punch(left_wrist, right_wrist, left_hip, right_hip, body_center_x, punch_threshold=0.1):
    global left_punching, right_punching

    # Right arm punch detection
    right_punch_now = right_hip.y - right_wrist.y > punch_threshold
    if right_punch_now and not right_punching:
        if right_wrist.x < body_center_x:
            print("Punch to the LEFT (right hand)")
        else:
            print("Punch to the RIGHT (right hand)")
        right_punching = True
    elif not right_punch_now:
        right_punching = False

    # Left arm punch detection
    left_punch_now = left_hip.y - left_wrist.y > punch_threshold
    if left_punch_now and not left_punching:
        if left_wrist.x < body_center_x:
            print("Punch to the LEFT (left hand)")
        else:
            print("Punch to the RIGHT (left hand)")
        left_punching = True
    elif not left_punch_now:
        left_punching = False

def detect_jump(current_left_y, current_right_y, current_time):
    global prev_left_foot_y, prev_right_foot_y, left_foot_velocities, right_foot_velocities, last_jump_time
    jump_detected = False

    if prev_left_foot_y is not None:
        left_velocity = prev_left_foot_y - current_left_y  # positive when moving up
        right_velocity = prev_right_foot_y - current_right_y

        left_foot_velocities.append(left_velocity)
        right_foot_velocities.append(right_velocity)

        if len(left_foot_velocities) > velocity_window:
            left_foot_velocities.pop(0)
            right_foot_velocities.pop(0)

        if len(left_foot_velocities) == velocity_window:
            avg_left_velocity = np.mean(left_foot_velocities)
            avg_right_velocity = np.mean(right_foot_velocities)

            if (current_time - last_jump_time > jump_cooldown and
                avg_left_velocity > foot_velocity_threshold and 
                avg_right_velocity > foot_velocity_threshold):
                print("JUMP!")
                last_jump_time = current_time
                jump_detected = True

    prev_left_foot_y = current_left_y
    prev_right_foot_y = current_right_y
    return jump_detected


# --- Walking detection state (reverted to original logic) ---
prev_left_knee_y = None
prev_right_knee_y = None
prev_left_ankle_y = None
prev_right_ankle_y = None
walking_state = False
walking_cooldown = 0
WALKING_COOLDOWN_FRAMES = 5  # Number of frames to keep walking after motion stops
MOTION_THRESHOLD = 0.025      # Minimum movement to count as motion (original value)
walking_direction = None  # 'left', 'right', or None

def detect_walking(landmarks):
    global prev_left_knee_y, prev_right_knee_y, prev_left_ankle_y, prev_right_ankle_y
    global walking_state, walking_cooldown, walking_direction

    left_knee_y = landmarks[mp_pose.PoseLandmark.LEFT_KNEE].y
    right_knee_y = landmarks[mp_pose.PoseLandmark.RIGHT_KNEE].y
    left_ankle_y = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE].y
    right_ankle_y = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE].y

    # Only use knees and ankles for walking detection
    if prev_left_knee_y is None:
        prev_left_knee_y = left_knee_y
        prev_right_knee_y = right_knee_y
        prev_left_ankle_y = left_ankle_y
        prev_right_ankle_y = right_ankle_y
        walking_direction = None
        return False

    left_knee_motion = abs(left_knee_y - prev_left_knee_y)
    right_knee_motion = abs(right_knee_y - prev_right_knee_y)
    left_ankle_motion = abs(left_ankle_y - prev_left_ankle_y)
    right_ankle_motion = abs(right_ankle_y - prev_right_ankle_y)

    # Only consider leg movement for walking
    motion_detected = (
        (left_knee_motion > MOTION_THRESHOLD or right_knee_motion > MOTION_THRESHOLD or
         left_ankle_motion > MOTION_THRESHOLD or right_ankle_motion > MOTION_THRESHOLD)
    )

    if motion_detected:
        walking_state = True
        walking_cooldown = WALKING_COOLDOWN_FRAMES
    else:
        if walking_cooldown > 0:
            walking_cooldown -= 1
        else:
            walking_state = False

    prev_left_knee_y = left_knee_y
    prev_right_knee_y = right_knee_y
    prev_left_ankle_y = left_ankle_y
    prev_right_ankle_y = right_ankle_y

    left_hip_z = landmarks[mp_pose.PoseLandmark.LEFT_HIP].z
    right_hip_z = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].z
    if walking_state:
        if right_hip_z < left_hip_z:  # Right hip is closer (smaller z value)
            walking_direction = 'left'   # Walking towards the left
        elif left_hip_z < right_hip_z:  # Left hip is closer (smaller z value)
            walking_direction = 'right'  # Walking towards the right
        else:
            walking_direction = None
    else:
        walking_direction = None

    return walking_state

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
            # if detect_step(landmarks):
            #     cv2.putText(image, "Step Forward!", (50, 100), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
            #     print("Step forward!")

            current_time = time.time()
            if detect_kick(landmarks, current_time):
                cv2.putText(image, "Kick!", (50, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                # Direction is printed in the detect_kick function


            # --- Punch Detection ---
            left_wrist = landmarks[mp_pose.PoseLandmark.LEFT_WRIST]
            right_wrist = landmarks[mp_pose.PoseLandmark.RIGHT_WRIST]
            left_hip = landmarks[mp_pose.PoseLandmark.LEFT_HIP]
            right_hip = landmarks[mp_pose.PoseLandmark.RIGHT_HIP]
            body_center_x = (left_hip.x + right_hip.x) / 2
            if detect_punch(left_wrist, right_wrist, left_hip, right_hip, body_center_x):
                cv2.putText(image, "punch!", (50, 200), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
                print("punch!")

            # --- Jump Detection ---
            left_ankle = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE]
            right_ankle = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE]
            import time
            current_time = time.time()
            if detect_jump(left_ankle.y, right_ankle.y, current_time):
                cv2.putText(image, "Jump!", (50, 250), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
                print("Jump!")

            if detect_walking(landmarks):
                direction_str = ""
                if walking_direction == 'left':
                    direction_str = " Left"
                elif walking_direction == 'right':
                    direction_str = " Right"
                cv2.putText(image, f"Walking!{direction_str}", (50, 300), cv2.FONT_HERSHEY_SIMPLEX, 1, (128, 0, 128), 2)
                #print(f"Walking!{direction_str}")

        # Display image
        cv2.imshow('MediaPipe Pose', image)

        if cv2.waitKey(10) & 0xFF == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
