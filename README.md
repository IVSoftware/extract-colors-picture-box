# Extract Colors from Picture Box

The source project has several issues. Let's take them one by one.

___
**Bottleneck**

The OP is wants to update a progress bar. But the reason for needing one seems to be the terrible bottleneck caused by `Contains` in this method.



