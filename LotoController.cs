using Microsoft.AspNetCore.Mvc;
using Shield.Common.Models.Common;
using Shield.Common.Models.Loto;
using Shield.Services.Loto.Middleware;
using Shield.Services.Loto.Models.Loto;
using Shield.Services.Loto.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shield.Services.Loto.Controllers
{
    [Route("api/[controller]")]
    public class LotoController : Controller
    {
        private LotoService _lotoService;
        
        public Shield.Common.Models.Loto.Shared.Loto LotoModel { get; private set; }

        public LotoController(LotoService lotoService)
        {
            _lotoService = lotoService;            
        }

        [HttpGet("Program/{model}/LineNumber/{lineNumber}")]
        //"{model}/{lineNumber}"
        public HTTPResponseWrapper<List<LotoDetails>> GetLotoByModelLineNumber(string model, string lineNumber)
        {
            var lotoList = _lotoService.GetLotosByModelAndLineNumber(model, lineNumber);
            var response = new HTTPResponseWrapper<List<LotoDetails>>
            {
                Status = "Success",
                Message = "LOTOs retrieved",
                Data = lotoList
            };
            return response;
        }

        [ApiKeyAuth]
        [HttpPost]
        public HTTPResponseWrapper<LotoDetails> Create([FromBody]Shield.Services.Loto.Models.Common.CreateLotoRequest createLotoRequest)
        {
            try
            {
                LotoDetails result = _lotoService.Create(createLotoRequest);

                return new HTTPResponseWrapper<LotoDetails>
                {
                    Status = "Success",
                    Message = $"Created new LOTO.",
                    Data = result
                };
            }
            catch (Exception e)
            {
                return new HTTPResponseWrapper<LotoDetails>
                {
                    Status = "Failed",
                    Message = "Failed to save LOTO",
                    Data = null
                };
            }
        }

        [ApiKeyAuth]
        [HttpPost("FromDiscrete")]
        public HTTPResponseWrapper<LotoDetails> CreateLotoFromDiscrete([FromBody] Shield.Services.Loto.Models.Common.CreateLotoRequest createLotoRequest)
        {
            try
            {
                LotoDetails result = _lotoService.CreateLotoFromDiscrete(createLotoRequest);

                return new HTTPResponseWrapper<LotoDetails>()
                {
                    Status = "Success",
                    Message = "Created new LOTO from Discrete.",
                    Data = result
                };
            }
            catch (Exception e)
            {
                return new HTTPResponseWrapper<LotoDetails>()
                {
                    Status = "Failed",
                    Message = "Failed to create LOTO from Discrete.",
                    Data = null
                };
            }
        }

        [ApiKeyAuth]
        [HttpPut]
        public HTTPResponseWrapper<LotoDetails> Update([FromBody]LotoDetails loto)
        {
            //Existing data from UI has to be saved to the properties from UI
            LotoServiceHelpers.ManipulateLotoDetails(loto);
            LotoDetails updated = _lotoService.Update(loto);
            if(updated != null && updated.LotoAssociatedHecps.Count > 0)
            {
                _lotoService.UpdateHecpIsolationtag(updated);
            }

            if (updated == null)
            {
                return new HTTPResponseWrapper<LotoDetails>
                {
                    Status = "Failed",
                    Message = $"LOTO for Line {loto.LineNumber} failed to update!",
                    Data = updated
                };
            }
            else
            {
                return new HTTPResponseWrapper<LotoDetails>
                {
                    Status = "Success",
                    Message = $"LOTO for Line {loto.LineNumber} has been updated!",
                    Data = updated
                };
            }
        }

        [HttpPost("DeleteLoto/{Id}")]
        [ApiKeyAuth]
        public IActionResult DeleteLoto(int Id)
        {
            try
            {
                bool isDeleted = _lotoService.DeleteLoto(Id);

                if (isDeleted)
                {
                    return new ObjectResult("Successfully deleted the loto " ) { StatusCode = 200 };
                }
                else
                {
                    return new ObjectResult("Failed to delete Loto.") { StatusCode = 500 };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new ObjectResult("Failed to delete Loto.") { StatusCode = 500 };
            }
        }   

        [HttpGet]
        [Route("LotoDetail/LotoId/{id}")]
        //"LotoDetail/{id}"
        public HTTPResponseWrapper<LotoDetails> LotoDetail(int id)
        {
            try
            {
                var res = _lotoService.GetLotoById(id);
                if (res == null)
                {
                    return new HTTPResponseWrapper<LotoDetails>
                    {
                        Status = "Failed",
                        Message = $"Could not find Loto with Id 123",
                        Data = null
                    };
                }
                else
                {
                    return new HTTPResponseWrapper<LotoDetails>
                    {
                        Status = "Success",
                        Message = "",
                        Data = res
                    };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LotoDetail - {ex.Message}");

                return new HTTPResponseWrapper<LotoDetails>
                {
                    Status = "Failed",
                    Message = $"Failed to find Loto with id {id}",
                    Data = null
                };
            }
        }

        [ApiKeyAuth]
        [HttpPost("PAESignIn")]
        //"SignInPAE"
        public async Task<IActionResult> SignInPAE([FromBody] SigningPAERequest signInPAERequest)
        {
            try
            {
                HTTPResponseWrapper<LotoDetails> result = await _lotoService.SignInPAE(signInPAERequest);
                return Ok(result);
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return Ok(new HTTPResponseWrapper<SigningPAERequest>()
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.TRAINING,
                    Message = $"Unable to verify {signInPAERequest.PAEName} has completed required trainings.",
                    Data = signInPAERequest
                });
            }
            catch (Exception)
            {
                return Ok(new HTTPResponseWrapper<LotoDetails>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.EXCEPTION_OCCURRED,
                    Message = $"Failed to sign in PAE with BEMSID {signInPAERequest.BemsId}",
                    Data = null
                });
            }
        }

        [ApiKeyAuth]
        //[Authorize]
        [HttpPost("AESignIn")]
        //"SignInAE"
        public async Task<IActionResult> SignInAE([FromBody]SigningAERequest signingAERequest)
        {
            try
            {
                HTTPResponseWrapper<LotoAE> result = await _lotoService.SignInAE(signingAERequest);

                return Ok(result);
            }
            catch (TaskCanceledException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return Ok(new HTTPResponseWrapper<SigningAERequest>()
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.TRAINING,
                    Message = $"Unable to verify {signingAERequest.AEName} has completed required trainings.",
                    Data = signingAERequest
                });
            }
            catch (Exception)
            {
                return Ok(new HTTPResponseWrapper<LotoAE>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.EXCEPTION_OCCURRED,
                    Message = $"Failed to sign in AE with BEMSID {signingAERequest.AEBemsId}",
                    Data = null
                });
            }
        }

        [HttpGet]
        [Route("LotoAEs/LotoId/{lotoId}")]
        //"LotoAEs/{lotoId}"
        public HTTPResponseWrapper<List<LotoAE>> GetAEsByLotoId(int lotoId)
        {
            try
            {
                var aeList = _lotoService.GetAEsByLotoId(lotoId);

                return new HTTPResponseWrapper<List<LotoAE>>
                {
                    Status = "Success",
                    Data = aeList,
                    Message = ""
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception thrown when retrieving list of AEs" + ex);
                return new HTTPResponseWrapper<List<LotoAE>>
                {
                    Status = "Failed",
                    Message = "Error retrieving list of AEs",
                    Data = null
                };
            }
        }

        [ApiKeyAuth]
        [HttpPut]
        [Route("AESignOut")]
        //"SignOutAE"
        public IActionResult SignOutAE([FromBody]SigningAERequest request)
        {
            try
            {
                List<LotoAE> updatedAes = _lotoService.SignOutAE(request.LotoId, request.AEBemsId, request.AEName, request.UserBemsId, request.UserName);
                if (updatedAes != null)
                {
                    return new ObjectResult(updatedAes);
                }
                else
                {
                    Console.Error.WriteLine("Sign Out AE returned null");
                    return new ObjectResult("AE Not Found") { StatusCode = 304 };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        //[Authorize]
        [ApiKeyAuth]
        [HttpPost("VisitorSignIn")]
        //"SignInAE"
        public async Task<IActionResult> SignInVisitor([FromBody]SigningAERequest signingAERequest)
        {
            try
            {
                HTTPResponseWrapper<LotoAE> result = await _lotoService.SignInVisitor(signingAERequest);

                return Ok(result);
            }
            catch (Exception)
            {
                return Ok(new HTTPResponseWrapper<LotoAE>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.EXCEPTION_OCCURRED,
                    Message = $"Failed to sign in Visitor with Name {signingAERequest.AEName}",
                    Data = null
                });
            }
        }

        [ApiKeyAuth]
        [HttpPut]
        [Route("VisitorSignOut")]
        //"SignOutAE"
        public IActionResult SignOutVisitor([FromBody]SigningAERequest request)
        {
            try
            {
                List<LotoAE> updatedAes = _lotoService.SignOutVisitor(request.LotoId, request.AEBemsId, request.AEName, request.UserBemsId, request.UserName);
                if (updatedAes != null)
                {
                    return new ObjectResult(updatedAes);
                }
                else
                {
                    Console.Error.WriteLine("Sign Out Non Boeing AE returned null");
                    return new ObjectResult("Non Boeing AE Not Found") { StatusCode = 304 };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        [HttpPost]
        [Route("AirplaneLotoCount")]
        public IActionResult AirplaneLotoCounts([FromBody] List<Shield.Services.Loto.Models.Common.Aircraft> aircraft)
        {
            try
            {
                // Dictionary<Airplane Id, Loto Count>
                Dictionary<int, int> airplaneIdToLotoDictionary = _lotoService.GetAirplaneLotoCounts(aircraft);

                return new ObjectResult(airplaneIdToLotoDictionary) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        [HttpGet]
        [Route("ActiveIsolations/Program/{program}/LineNumber/{lineNumber}")]
        //"GetActiveIsolations/{program}/{lineNumber}"
        public ObjectResult GetLotosWithActiveIsolationsByProgramAndLine(string program, string lineNumber)
        {
            try
            {
                List<LotoDetails> isos = _lotoService.GetLotosWithActiveIsolationsByProgramAndLine(program, lineNumber);

                return new ObjectResult(isos) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception thrown in get active isolations " + ex);
                return new ObjectResult($"Failed to get Active Isolations for {program} line {lineNumber}") { StatusCode = 500 };
            }
        }

        [HttpPost]
        [Route("ConflictIsolations/LotoId/{lotoId}/program/{program}/lineNumber/{lineNumber}")]
        public ObjectResult GetConflictIsolations([FromBody] List<int> hecpIds, int lotoId, string program, string lineNumber)
        {
            try
            {
                List<Shield.Services.Loto.Models.Hecp.HecpIsolationTag> isoList = _lotoService.GetConflictIsolations(lotoId, program, hecpIds, lineNumber);
                return new ObjectResult(isoList) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception thrown in get active isolations " + ex);
                return new ObjectResult($"Failed to get Isolations for {lotoId}") { StatusCode = 500 };
            }
        }

        [HttpGet]
        [Route("IsHecpDeletable/HecpId/{hecpId}")]
        public HTTPResponseWrapper<bool> IsHecpDeletable(int hecpId)
        {
            try
            {
                bool isHecpDeletable = _lotoService.IsHecpDeletable(hecpId);

                return new HTTPResponseWrapper<bool>
                {
                    Status = Shield.Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                    Data = isHecpDeletable,
                    Message = ""
                };
            }
            catch (Exception ex)
            {
                string errorMessage = "Exception thrown when checking if the HECP is deletable";
                Console.Error.WriteLine(errorMessage + ex);
                return new HTTPResponseWrapper<bool>
                {
                    Status = Shield.Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Data = false,
                    Message = errorMessage
                };
            }
        }

        [ApiKeyAuth]
        [HttpPut]
        [Route("UpdateJobInfo")]
        public HTTPResponseWrapper<LotoDetails> UpdateLotoJobInfo([FromBody] Models.Common.CreateLotoRequest lotoJobInfo)
        {
            try
            {
                LotoDetails updated = _lotoService.UpdateLotoJobInfo(lotoJobInfo);

                if (updated != null)
                {
                    return new HTTPResponseWrapper<LotoDetails>
                    {
                        Status = Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                        Message = $"Work Package and Reason for LOTO have been updated!",
                        Data = updated
                    };
                }
                else
                {
                    return new HTTPResponseWrapper<LotoDetails>
                    {
                        Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                        Message = $"Failed to update Work Package and Reason!",
                        Data = updated
                    };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception occured while updating the Work Package and Reason." + ex);
                return new HTTPResponseWrapper<LotoDetails>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.FAILED,
                    Message = "Error updating Work Package and Reason!",
                    Data = null
                };
            }
            
        }
    }
}
