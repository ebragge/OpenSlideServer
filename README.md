## OpenSlideServer

OpenSlideServer provides a thin C# layer on top of the OpenSlide C libraries to show details and create a PNG image 
from an interest region of a high dimension image.

[**OpenSlide**](https://openslide.org/) is a C library that provides a simple interface to read whole-slide images (also known as virtual slides). 
OpenSlide and its official language bindings are released under the terms of the GNU Lesser General Public License, version 2.1.

[OpenSlide binaries](https://openslide.org/download/) 

Test images:
[http://openslide.cs.cmu.edu/download/openslide-testdata/](http://openslide.cs.cmu.edu/download/openslide-testdata/)

**Configuration** (config.json):

```json
    {
        "location": "C:\\Users\\Public\\OpenSlide\\",
        "prefixes": [
            "http://localhost:8081/", 
            "http://127.0.0.1:8081/"
        ]
    }
```

![List](/Images/list.PNG)

![Details](/Images/details.PNG)

![Image](/Images/image.PNG)
