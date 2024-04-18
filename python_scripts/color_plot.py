import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

# Circle parameters
theta = np.linspace(0, 2 * np.pi, 100)
r = 2  # Radius of the circle
x = r * np.cos(theta)  # x coordinates
y = r * np.sin(theta)  # y coordinates
z = np.zeros(len(theta))  # Keep the circle in the XY plane, thus z=0

# Initialize the colors array
colors = np.zeros((len(theta), 3))  # RGB colors

for i in range(len(theta)):
    # Normalize the angle for color mapping
    angle_norm = (np.cos(theta[i]) + 1) / 2  # Maps cos(theta) to [0, 1]
    angle_sin_norm = (np.sin(theta[i]) + 1) / 2  # Maps sin(theta) to [0, 1]
    
    # Calculate color components
    green = angle_norm if np.cos(theta[i]) > 0 else (1 - angle_norm)
    blue = angle_sin_norm if np.sin(theta[i]) > 0 else 0
    red = (1 - angle_sin_norm) if np.sin(theta[i]) < 0 else 0
    
    # Assign colors
    colors[i] = [red, green, blue]

# Plotting
fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

# Plot each point with its calculated color
for i in range(len(x)):
    ax.scatter(x[i], y[i], z[i], color=colors[i], s=10)

# Add a vertical axis in the middle of the circle
z_axis_min, z_axis_max = -1.5, 1.5
ax.plot([0, 0], [0, 0], [z_axis_min, z_axis_max], color='black', lw=2)

# Remove default axis labels, ticks, and tick labels
ax.set_xticks([])
ax.set_yticks([])
ax.set_zticks([])
ax.set_xlabel('')
ax.set_ylabel('')
ax.set_zlabel('')

# Custom degree annotations placed at traditional axis label positions
degree_annotations = {
    "0°": (1.5, 0, 0),
    "90°": (0, 1.5, 0),
    "180°": (-1.5, 0, 0),
    "-90°": (0, -1.5, 0)
}

# Add custom degree annotations for clarity
for text, pos in degree_annotations.items():
    ax.text(pos[0], pos[1], pos[2], text, color='black', fontsize=10, ha='center', va='center')

# Setting plot appearance
#ax.set_title('Color mapping used for LEDs')

# Adjust plot limits to accommodate annotations
ax.set_xlim([-2, 2])
ax.set_ylim([-2, 2])
ax.set_zlim([z_axis_min, z_axis_max])

plt.show()
