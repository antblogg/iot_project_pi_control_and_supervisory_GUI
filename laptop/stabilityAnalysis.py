import scipy.signal as signal
import control
import matplotlib.pyplot as plt
import numpy as np

tau = 3.0

num_pade, den_pade = control.pade(tau, 3)
Ku = 4.7
Ka = 0.67
num_plant = [Ku]
den_plant = [1, Ka]

num = signal.convolve(num_plant, num_pade)
den = signal.convolve(den_plant, den_pade)
num = num_plant
den = den_plant

dt = 0.1
w = np.logspace(-3, np.log10(2 / dt), 1500)

ctrl_sys = control.TransferFunction(num, den)
ctrl_sys = control.sample_system(ctrl_sys, dt, method="zoh")

sys = signal.dlti(
    ctrl_sys.num[0][0],
    ctrl_sys.den[0][0],
    dt=dt,
)

w, mag, phase = signal.dbode(sys, w=w)

plt.style.use("seaborn-v0_8-whitegrid")

fig, (ax_mag, ax_phase) = plt.subplots(
    2, 1, figsize=(10, 8), sharex=True
)
fig.suptitle("Bode Plot", fontsize=16)

ax_mag.semilogx(w, mag, linewidth=2)
ax_mag.set_ylabel("Magnitude [dB]")
ax_mag.grid(True, which="both", linestyle="--", alpha=0.7)

ax_phase.semilogx(w, phase, linewidth=2, color="tab:orange")
ax_phase.set_xlabel("Frequency [rad/s]")
ax_phase.set_ylabel("Phase [deg]")
ax_phase.grid(True, which="both", linestyle="--", alpha=0.7)

fig.tight_layout()

"""
plt.figure(figsize=(8, 8))
control.pole_zero_plot(
    ctrl_sys,
    grid=False,
    title="Pole-Zero Map",
)
"""
plt.show()