using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.DTO;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace DatingApp.API.Controllers
{
    [Route("api/users/{userId}/photos")]
    [Controller]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repository;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repository,
            IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret);
            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int photoId)
        {
            Photo photoFromRepo = await _repository.GetPhoto(photoId);
            PhotoForReturnDTO photo = _mapper.Map<PhotoForReturnDTO>(photoFromRepo);
            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserPhoto (int userId, [FromForm]PhotoForCreationDTO photoForCreation) 
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }

            User userFromRepo = await _repository.GetUser(userId);
            IFormFile file = photoForCreation.File;
            ImageUploadResult uploadResult = new ImageUploadResult();
            if(file.Length > 0)
            {
                using(Stream stream = file.OpenReadStream())
                {
                    ImageUploadParams uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                                                .Width(500)
                                                .Height(500)
                                                .Crop("fill")
                                                .Gravity("face")
                    };
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            photoForCreation.Url = uploadResult.Uri.ToString();
            photoForCreation.PublicId = uploadResult.PublicId;

            Photo photo = _mapper.Map<Photo>(photoForCreation);

            if (!userFromRepo.Photos.Any(u => u.IsMain))
            {
                photo.IsMain = true;
            }

            userFromRepo.Photos.Add(photo);
            
            if (await _repository.SaveAll())
            {
                PhotoForReturnDTO photoToReturn = _mapper.Map<PhotoForReturnDTO>(photo);
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoToReturn);
            }
            return BadRequest("Could not add photo");
        }        

        [HttpPost("{id}/SetMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            User user = await _repository.GetUser(userId);

            if(!user.Photos.Any(p => p.Id == id))
            {
                return Unauthorized();
            }

            Photo photoFromRepo = await _repository.GetPhoto(id);

            if(photoFromRepo.IsMain)
            {
                return BadRequest("This is currently a main photo");
            }

            Photo currentMainPhoto = await _repository.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if(await _repository.SaveAll())
                return NoContent();

            return BadRequest("Could not set main photo");
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            User user = await _repository.GetUser(userId);

            if(!user.Photos.Any(p => p.Id == id))
            {
                return Unauthorized();
            }

            Photo photoFromRepo = await _repository.GetPhoto(id);

            if(photoFromRepo.IsMain)
            {
                return BadRequest("You can not delete a main photo");
            }

            if(photoFromRepo.PublicId != null)
            {
                DeletionResult result = _cloudinary.Destroy(new DeletionParams(photoFromRepo.PublicId));

                if (result.Result == "ok")
                {
                    _repository.Delete(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicId == null)
            {
                _repository.Delete(photoFromRepo);
            }            

            if (await _repository.SaveAll())
            {
                return Ok();
            }

            return BadRequest();
        }
    }
}