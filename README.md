# GHLidar
My original attempt at bringing in .LAS files directly into GH.

PDAL is a great library and [this c# wrapper was a great starting point](https://github.com/PDAL/CAPI) but I've decided to switch course for a few reason:

1. The compiled plugin was almost 2 GB.
2. I was using 1% of PDAL.
3. `DllImport` was having a very hard time locating the required DLL unless I buried the .GHA way deep in the huge PDAL folder structure.
4. I don't know enough C++ to get the *PDAL views* to work correctly with the `filters.splitter`. Large files always exceeded the byte[] max capacity when trying to get coordinates from views using the wrapper.

If you randomly stumbled upon this repo and are interested in its desired funcationality, check out [site_Reader](https://github.com/aarcThom/site_Reader) which has the same goals but uses [LASzip](https://github.com/LASzip/LASzip) to read .LAS files.
