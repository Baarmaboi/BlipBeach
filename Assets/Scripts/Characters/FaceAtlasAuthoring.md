# Face atlas

One row of images, shown by changing the face material offset.

| Index | Reed | Tara / Tom |
| --- | --- | --- |
| 0 | idle | idle |
| 1 | blink | blink |
| 2 | look left | look left |
| 3 | look right | look right |
| 4 | — | happy |

**Frames In Row:** Reed `4`, Tara and Tom `5`. Tiling X = `1 / frames`.

## Talk / events

`FaceAnimator.HoldFrame(index)` keeps that face on. `ClearHold()` goes back to idle.

On an NPC, `NPCInteract` has optional **Face** fields. Tara and Tom: Hold Face While Talking on, Talk Face Frame `4`. Leave those off on other NPCs.

Import: Wrap **Clamp**, Alpha Is Transparency on, mipmaps off.
