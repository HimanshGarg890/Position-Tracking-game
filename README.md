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
# scp the built binary over to your QNX target
scp ./nto/aarch64/o.le/camera_example1_callback root@<target-ip-address>:/system/xbin

# ssh into the target
ssh root@<target-ip-address>

# Make sure sensor service is running, if not start sensor:
# sensor -U 521:521,1001 -r /accounts/1000/shared/sensor -c /system/etc/system/config/sensor_demo.conf

# Run example; -u 1 means we want to use CAMERA_UNIT_1 which is specified in sensor_demo.conf
camera_example1_callback -u 1
```

You will see the following output:
```console
Channel averages: 84.772, 130.876, 129.243 took 3.472 ms (press any key to stop example)
```
