# camera_example1_callback

This example source code shows how you can interact with the sensor service using libcamapi. Sensor service interfaces with camera drivers to send user applications camera frames through libcamapi.

camera_example1_callback shows an example of how to use the callbackmode; every time there is a new buffer available, `processCameraData` gets called.

How to build
```bash
# Source your QNX SDP script
source ~/qnx800/qnxsdp-env.sh

# Clone the repository
git clone https://gitlab.com/qnx/apps/camera_example1_callback.git && cd camera_example1_callback

# Build and install
make install
```

How to run
```bash
# scp libraries and tests to the target (note, mDNS is configured from
# /boot/qnx_config.txt and uses qnxpi.local by default).
TARGET_HOST=<target-ip-address-or-hostname>

# scp the built binary over to your QNX target
scp ./nto/aarch64/o.le/camera_example1_callback qnxuser@$TARGET_HOST:/data/home/qnxuser/bin

# ssh into the target
ssh qnxuser@$TARGET_HOST

# Run example; -u 1 means we want to use CAMERA_UNIT_1 which is specified in sensor_demo.conf
camera_example1_callback -u 1
```

You will see the following output:
```console
Channel averages: 84.772, 130.876, 129.243 took 3.472 ms (press any key to stop example)
```
