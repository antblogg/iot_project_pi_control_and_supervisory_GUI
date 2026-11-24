import time


class PIDController:
    def __init__(self, kp=1.0, ki=0.0, kd=0.0, setpoint=25.0, output_limits=(0.0, 100.0)):
        # Parameters can be changed dynamically at runtime (e.g. pid.kp = 2.0)
        self.kp = float(kp)
        self.ki = float(ki)
        self.kd = float(kd)
        self.setpoint = float(setpoint)
        self.output_limits = output_limits

        self._integral = 0.0
        self._last_error = None
        self._last_time = None

    def reset(self):
        self._integral = 0.0
        self._last_error = None
        self._last_time = None

    def update(self, measurement, now=None):
        if now is None:
            now = time.monotonic()

        error = self.setpoint - float(measurement)

        if self._last_time is None:
            dt = 0.0
        else:
            dt = now - self._last_time
            if dt < 0.0:
                dt = 0.0

        proportional = self.kp * error

        if dt > 0.0:
            self._integral += error * dt
        integral_term = self.ki * self._integral

        derivative_term = 0.0
        if dt > 0.0 and self._last_error is not None:
            derivative_term = self.kd * (error - self._last_error) / dt

        output = proportional + integral_term + derivative_term

        low, high = self.output_limits
        if output > high:
            output = high
            # Anti-windup: clamp integral so it does not keep pushing output past limit
            if self.ki != 0.0:
                self._integral = (output - proportional - derivative_term) / self.ki
        elif output < low:
            output = low
            if self.ki != 0.0:
                self._integral = (output - proportional - derivative_term) / self.ki

        self._last_error = error
        self._last_time = now
        return output
