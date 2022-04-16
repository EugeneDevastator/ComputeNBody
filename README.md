# Compute N-Body
Simulating NBody via compute shader

Quick flow:
1. pass particles as array.
2. calculate forces in 2d texture
3. sum the matrix via fancy parallelization.
4. apply forces.

![image](https://user-images.githubusercontent.com/5610313/163670320-020f2071-3db2-4eb1-ad7d-95df549e7260.png)

