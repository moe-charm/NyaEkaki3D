# Synthetic PNG variants

Regenerate with `python Tools/Generate-PngVariants.py` from the repository root.
These are original mathematical patterns, with no avatar or external image data.

The 30 fixtures cover all PNG color type/bit depth combinations, each with normal
scanlines (cycling through all five PNG filters) and Adam7 interlacing. Dimensions
9 by 11 exercise all seven passes and packed rows with unused trailing bits.
Indexed fixtures include partial tRNS tables; gray/RGB include a transparent key;
gray-alpha and RGBA include independent partial alpha values.

The manifest's expected bottom-left RGBA bytes come from source samples before
encoding, not from NyaForge or Unity. 16-bit samples mostly equal 8-bit values
times 257. Gray/RGB16 also include sample 1 beside transparent sample 0: both
become black at 8 bits, but only the exact 16-bit transparent key has zero alpha.
This checks transparency matching before precision reduction.

`PngVariantVerification` loads these through the production importer in the
Windows Player and compares every decoded byte. Its output is `png-variants.txt`
in the authoring test artifact directory. These tests establish representative
decoding behavior, not full PNG standards conformance or ICC color conversion.

Reference: [PNG Third Edition](https://www.w3.org/TR/png-3/), interlacing,
filtering, sample packing and tRNS. Pillow 12.0.0 independently matched all 30
initial fixtures. After adding the low-bit transparent-key distinction, its
RGB16-to-RGBA conversion merges those two keys; it is therefore not used as the
oracle for those two fixtures. Unity's importer passes the source-derived values.
