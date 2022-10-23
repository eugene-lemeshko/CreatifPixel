# Using CreatifPixelApi.
## 1. Getting health of API-services.
```sh
Request:
Method: Get
URL: https://<baseApiURL>/api/health

Response:
{
  "status": "Healthy",
  "results": {}
}
```
Status values: 
- Healthy
- Unhealthy

## 2. Getting image previews.
```sh
Request:
Method: POST
URL: https://<baseApiURL>/api/image/get-preview
Headers: 'Access-Control-Allow-Origin': '*'
Payload:
{
    base64DataString: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAKA....",
    contrast: 0,
    size: 1
}

Response:
{
    base64StringImages: [
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD...",
        "data:image/jpg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD..."
    ],
    licenseKey: null,
    size: 1
}
```
Payload:
- base64DataString - base64 string with cropped to square body of image. Width should be equals to height. Supports bmp/tiff/jpeg/png/icon formats.
- contrast is left for future support. Set to 0.
- size: 0 - Small, 1 - Medium. Set to 1

Response:
-  base64StringImages - array of base64 strings with images preview. The size of the preview image depends on the request size parameter and could be the different.
-  licenseKey - ignore
-  size - ignore
-  
## 3. Getting PDF schema.
```sh
Request:
Method: POST
URL: https://<baseApiURL>/api/image/get-schema
Headers: 'Access-Control-Allow-Origin': '*'
Payload:
{
    base64DataString: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAKA....",
    contrast: 0,
    buildByIndex: <index of preview image>,
    size: 1,
    licenseKey: <license number>
}

Response:
Returns BLOB of PDF file 
```
Payload:
- base64DataString - base64 string with cropped to square body of image used in the previous request.
- contrast is left for future support. Set to 0.
- size: 0 - Small, 1 - Medium. Set to 1
- licenseKey - The license key the user should input. Test key = "Medium_190979"
- buildByIndex - Index of preview image from the previous request user selected. Starts from 0.

Response:
 File/Blob object.
 
 ## 4. Errors.
  Status Code: 400 Bad Request. Returns in response as string.
  - NO_IMAGE_BODY: image body is empty
  - WRONG_IMAGE_SIZE: image width isn't equal image height
  - NO_LICENSE_CODE: license code isn't presented or incorrect
  
 EXAMPLE:
 https://github.com/eugene-lemeshko/ImageBrickProto/tree/main/image-brick-proto
