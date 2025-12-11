using Azure.Core;
using Shield.Common.Constants;
using Shield.Common.Models.Common;
using Shield.Common.Models.Loto;
using Shield.Services.Loto.Data;
using Shield.Services.Loto.Models.Discrete;
using Shield.Services.Loto.Models.Hecp;
using Shield.Services.Loto.Models.Loto;
using Shield.Services.Loto.Services.Interfaces;
using Shield.Services.Loto.Translators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shield.Services.Loto.ShieldConstants;

namespace Shield.Services.Loto.Services
{
    public class LotoService
    {
        private ILotoDAO _lotoDao;
        private IStatusDAO _statusDao;
        private ILotoTransactionService _lotoTransactionService;
        private IsolationService _isolationService;
        private IDiscreteDAO _discreteDao;
        private IDiscreteService _discreteService;
        private IMyLearningDataService _myLearningDataService;
        private UserService _userService;
        private IEmailService _emailService;
        private readonly string _shield_Environment;
        private readonly string _shield_URL;
        private readonly string _footer;

        public LotoService(ILotoDAO dao, IStatusDAO statusDao, ILotoTransactionService lts, IsolationService iservice, IDiscreteDAO discreteDao, IDiscreteService discreteService, IMyLearningDataService myLearningDataService, UserService userService, IEmailService emailService)
        {
            _lotoDao = dao;
            _statusDao = statusDao;
            _lotoTransactionService = lts;
            _isolationService = iservice;
            _discreteDao = discreteDao;
            _discreteService = discreteService;
            _myLearningDataService = myLearningDataService;
            _userService = userService;
            _emailService = emailService;
            _shield_Environment = Environment.GetEnvironmentVariable(Constants.SHIELD_ENVIRONMENT);
            _shield_URL = Environment.GetEnvironmentVariable(Constants.SHIELD_URL);
            _footer = string.Format(Constants.EmailFooter,
                           !string.IsNullOrEmpty(_shield_Environment) ? _shield_Environment + " Environment" : string.Empty,
                           " (" + _shield_URL + ")");
        }

        public virtual List<LotoDetails> GetLotosByModelAndLineNumber(string model, string lineNumber)
        {
            return _lotoDao
                .GetLotos(model, lineNumber)
                .Select(loto =>
                {
                    return LotoTranslator.ToLotoSummary(loto);
                })
                .ToList();
        }

        public virtual LotoDetails Create(Shield.Services.Loto.Models.Common.CreateLotoRequest request)
        {
            DateTime now = DateTime.UtcNow;

            List<LotoAssociatedModelData> lotoAssociatedModelDataList = new List<LotoAssociatedModelData>();

            //if minor models are selected, insert all the records into LotoAssociatedModelData
            if (request.AssociatedMinorModelIdList?.Count > 0)
            {
                lotoAssociatedModelDataList.AddRange(request.AssociatedMinorModelIdList.Select(minorModelId => new LotoAssociatedModelData { MinorModelId = minorModelId }));
            }
            var lotoToCreate = new LotoDetails
            {
                LineNumber = request.LineNumber,
                Model = request.Model,
                Reason = request.Reason,
                Site = request.Site,
                WorkPackage = request.WorkPackage,
                CreatedByBemsId = request.CreatedByBemsId,
                Status = _statusDao.GetStatusById(1),
                LotoAssociatedModelDataList = lotoAssociatedModelDataList
            };

            var res = _lotoDao.Create(lotoToCreate);

            _lotoTransactionService.AddTransaction(res.Id, $"Loto for line {lotoToCreate.LineNumber} created by {request.CreatedByName} ({lotoToCreate.CreatedByBemsId})");
            _lotoTransactionService.AddTransaction(res.Id, $"{request.CreatedByName} ({request.CreatedByBemsId}) is the GC responsible for this Line #{request.LineNumber} at the time of this LOTO Creation");
            _lotoTransactionService.AddTransaction(res.Id, $"{request.CreatedByName} ({request.CreatedByBemsId}) Created LOTO with Work Package: {request.WorkPackage} and Reason: {request.Reason}");
            return res;
        }

        public virtual LotoDetails CreateLotoFromDiscrete(Shield.Services.Loto.Models.Common.CreateLotoRequest request)
        {
            LotoDetails loto = Create(request);

            StatusChangeRequest statusChangeRequest = new StatusChangeRequest
            {
                Id = request.Discrete.Id,
                BemsId = request.CreatedByBemsId,
                DisplayName = request.CreatedByName
            };

            Discrete discrete = AssignedToLoto(statusChangeRequest);

            loto.Discrete = discrete;

            loto = Update(loto);

            return loto;
        }


        public virtual async Task<HTTPResponseWrapper<LotoDetails>> SignInPAE(SigningPAERequest req)
        {
            HTTPResponseWrapper<LotoDetails> response ;

            LotoDetails lotoToUpdate = _lotoDao.GetLotoById(req.LotoId);
            DateTime now = DateTime.UtcNow;

            LotoDetails lotoData = _lotoDao.Update(lotoToUpdate);

            List<string> courseCodeList = new List<string>
                {
                    TrainingCourses.ANNUAL_REQUIRED_LOTO_FIELD_OBSERVATION_TRAINING,
                    TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL
                };
            TrainingInfo trainingInfo = await _myLearningDataService.GetMyLearningDataAsync(req.BemsId, courseCodeList);
            List<string> invalidTrainings = trainingInfo.MyLearningDataResponse.Where(x => !x.IsTrainingValid).Select(x => x.CertCode).ToList();

            bool isAllTrainingValid = !invalidTrainings.Any();
            string trainingText = invalidTrainings.Count > 1 ? "trainings" : "training";
            string trainingCodes = string.Join(", ", invalidTrainings);

            if (lotoToUpdate != null && (req.OverrideTraining || isAllTrainingValid))
            {
                    lotoToUpdate.AssignedPAEBems = req.BemsId;
                    lotoToUpdate.PAESignInTime = now;

                    if (lotoToUpdate.Status.Description == "Transfer")
                    {
                        lotoToUpdate.Status = _statusDao.GetStatusByDescription("Active");

                            if (req.OverrideTraining)
                            {
                                _lotoTransactionService.AddTransaction(req.LotoId, $"{req.PAEName} ({req.BemsId}) signed in as PAE. Training ({trainingCodes}) was overrided because :: {req.ReasonToOverrideTriaining} ");
                                _lotoTransactionService.AddTransaction(req.LotoId, $"{req.PAEName} ({req.BemsId}) changed LOTO status to Active");
                                if (req.BemsId == req.GcBemsId)
                                {
                                    var isEmailSent = SendEmailToPAEManager(req.LotoId, req.BemsId, req.GcBemsId, req.ReasonToOverrideTriaining, trainingCodes);
                                     return response = new HTTPResponseWrapper<LotoDetails>
                                    {
                                        Status = Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                                        Message = $"Signed in {req.PAEName} as an PAE.Email has been sent to your Manager for overriding the {trainingText}.",
                                        Data = lotoData
                                    };
                                }
                            }
                            else
                            {
                                _lotoTransactionService.AddTransaction(req.LotoId, $"{req.PAEName} ({req.BemsId}) signed in as PAE.");
                                _lotoTransactionService.AddTransaction(req.LotoId, $"{req.PAEName} ({req.BemsId}) changed LOTO status to Active");
                            }
                        }
                    else
                    {
                            if (req.OverrideTraining)
                            {
                                _lotoTransactionService.AddTransaction(req.LotoId, $"{req.PAEName} ({req.BemsId}) signed in as PAE. Training ({trainingCodes}) was overrided because :: {req.ReasonToOverrideTriaining} ");
                                if (req.BemsId == req.GcBemsId)
                                {
                                    var isEmailSent = SendEmailToPAEManager(req.LotoId, req.BemsId, req.GcBemsId, req.ReasonToOverrideTriaining, trainingCodes);
                                    return response = new HTTPResponseWrapper<LotoDetails>
                                    {
                                        Status = Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                                        Message = $"Signed in {req.PAEName} as an PAE.Email has been sent to your Manager for overriding the {trainingText}.",
                                        Data = lotoData
                                    };
                                }
                            }
                            else
                            {
                                _lotoTransactionService.AddTransaction(req.LotoId, $"{req.PAEName} ({req.BemsId}) signed in as PAE.");
                            }
                    };
                
                response = new HTTPResponseWrapper<LotoDetails>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                    Message = $"Signed in {req.PAEName} as an PAE.",
                    Data = lotoData
                };
            }
            else
            {
                lotoData.UserTrainingData = trainingInfo.MyLearningDataResponse;
                response = new HTTPResponseWrapper<LotoDetails>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.TRAINING,
                    Message = $"{req.PAEName} does not have the required {trainingText} ({trainingCodes}).",
                    Data = lotoData
                };
            }

            return response;
        }

        public virtual async Task<bool> SendEmailToPAEManager(int lotoId, int paeBemsId, int gcBemsId, string reason, string trainingCodes)
        {
            LotoDetails loto = _lotoDao.GetLotoById(lotoId);
            Models.Users.User managerDetails = await _userService.GetManagerDetails(paeBemsId);
            Models.Users.User pae = await _userService.GetUserByBemsId(paeBemsId);
            Models.Email.EmailRequest emailRequest = new Models.Email.EmailRequest();
            emailRequest.Subject = $"Mandatory LOTO Training Override for {pae.DisplayName} in Shield {_shield_Environment}";
            emailRequest.IsHtml = true;
            emailRequest.Body = $@" <html>
	                                <head></head>
                                    {Constants.EmailHeader}
	                                <body style='font-family:Calibri,sans-serif;'>
                                        <br />
										Hi {managerDetails.DisplayName}, 
										<br />
                                        <br />
                                        <b>{pae.DisplayName} </b> claimed the LOTO - <b>{loto.WorkPackage}</b> as PAE in Site {loto.Site}, Program {loto.Model}, Line Number {loto.LineNumber}.
                                        <br/>
                                        Training(s) ({trainingCodes}) has been overridden with the reason : <b>{reason}</b>.
                                        <br />
                                        <br />
		                                You are receiving this email because you are the Manager of <b>{pae.DisplayName}</b> and mandatory training has been overridden.
                                        <br />
                                        <br />
                                        ACTION : Please follow up with the employee to complete the incomplete mandatory trainings as mentioned above.
                                        <br />
                                        <br />
                                        <hr />
		                                {_footer}
	                                </body>
                                    </html>";
            emailRequest.To.Add(managerDetails.EmailAddress);
            var result = _emailService.SendEmail(emailRequest);
            return result;
        }

        public virtual LotoDetails Update(LotoDetails loto)
        {
            return _lotoDao.Update(loto);
        }

        public void UpdateHecpIsolationtag(LotoDetails loto)
        {
            _lotoDao.UpdateHecpIsolationtag(loto);
        }

        public virtual bool DeleteLoto(int lotoId)
        {
            var loto = GetLotoById(lotoId);

            if (loto is not null)
            {
               return _lotoDao.DeleteLoto(loto);
              
            }
            return false;
        }

        public virtual LotoDetails SignOutPAE(int LotoId, string displayName, int bemsId)
        {
            LotoDetails loto = GetLotoById(LotoId);
            loto.AssignedPAEBems = null;
            loto.PAESignInTime = null;
            LotoDetails res = Update(loto);

            _lotoTransactionService.AddTransaction(LotoId, $"{displayName} ({bemsId}) signed out as PAE");
            return res;
        }

        public virtual LotoDetails GetLotoById(int id)
        {
            LotoDetails loto = _lotoDao.GetLotoById(id);
            loto.Discrete = loto.Discrete == null ? null : _discreteDao.GetDiscreteById(loto.Discrete.Id);

            return loto;
        }

        public virtual async Task<HTTPResponseWrapper<LotoAE>> SignInAE(SigningAERequest request)
        {
            HTTPResponseWrapper<LotoAE> response;
            LotoDetails loto = _lotoDao.GetLotoById(request.LotoId);
            LotoDetails lotoAESignedInto = _lotoDao.LotoAESignedInto(request);

            if (lotoAESignedInto == null)
            {
                LotoAE ae = new LotoAE
                {
                    AEBemsId = request.AEBemsId,
                    LotoId = request.LotoId,
                    SignInTime = DateTime.UtcNow
                };

                List<string> courseCodeList = new List<string>
                        {
                            TrainingCourses.ANNUAL_REQUIRED_LOTO_FIELD_OBSERVATION_TRAINING,
                            TrainingCourses.AIRCRAFT_HAZARDOUS_ENERGY_CONTROL
                        };
                TrainingInfo trainingInfo = await _myLearningDataService.GetMyLearningDataAsync(ae.AEBemsId, courseCodeList);

                var invalidTrainings = trainingInfo.MyLearningDataResponse.Where(x => !x.IsTrainingValid).Select(x => x.CertCode).ToList();

                bool isAllTrainingValid = !invalidTrainings.Any();
                string trainingText = invalidTrainings.Count > 1 ? "trainings" : "training";
                string trainingCodes = string.Join(", ", invalidTrainings);

                if (request.OverrideTraining || isAllTrainingValid)
                {
                    var result = _lotoDao.SignInAE(ae);
                    Update(result.Loto);

                    var transactionString = $"{request.UserName} ({request.UserBemsId}) signed in {request.AEName} ({request.AEBemsId}) as an AE";
                    if(request.OverrideTraining)
                    {
                        transactionString = $"{request.UserName} ({request.UserBemsId}) signed in {request.AEName} ({request.AEBemsId}) as an AE. Training ({trainingCodes}) was overriden by ({request.UserName})  because :: {request.ReasonToOverrideTriaining} ";
                    }
                    _lotoTransactionService.AddTransaction(result.LotoId, transactionString);

                    response = new HTTPResponseWrapper<LotoAE>
                    {
                        Status = Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                        Message = $"Signed in {request.AEName} as an AE.",
                        Data = result
                    };
                }
                else
                {
                    response = new HTTPResponseWrapper<LotoAE>
                    {
                        Status = Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED,
                        Reason = Common.Constants.ShieldHttpWrapper.Reason.TRAINING,
                        Message = $"{request.AEName} does not have the required {trainingText} ({trainingCodes}).",
                        Data = new LotoAE
                        {
                            AEBemsId = request.AEBemsId,
                            LotoId = request.LotoId,
                            UserTrainingData = trainingInfo.MyLearningDataResponse
                        }
                    };
                }
            }
            else
            {
                response = new HTTPResponseWrapper<LotoAE>
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.ALREADY_EXISTS,
                    Message = $"{request.AEName} cannot sign into this LOTO because they are signed into a LOTO on {lotoAESignedInto.Model} Line #{lotoAESignedInto.LineNumber}",
                    Data = null
                };
            }

            return response;
        }

        public virtual async Task<HTTPResponseWrapper<LotoAE>> SignInVisitor(SigningAERequest request)
        {
            HTTPResponseWrapper<LotoAE> response;

            LotoAE lotoVisitorSignedInto = _lotoDao.LotoVisitorSignedInto(request);

            if (lotoVisitorSignedInto == null)
            {
                LotoAE ae = new LotoAE()
                {
                    AEBemsId = request.AEBemsId,
                    LotoId = request.LotoId,
                    SignInTime = DateTime.UtcNow,
                    FullName = request.AEName
                };

                var result = _lotoDao.SignInVisitor(ae);
                //Update(result.Loto);

                _lotoTransactionService.AddTransaction(result.LotoId, $"{request.UserName} ({request.UserBemsId}) signed in {request.AEName} as a Non Boeing Visitor AE");

                response = new HTTPResponseWrapper<LotoAE>()
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.SUCCESS,
                    Message = $"Signed in {request.AEName} as a Visitor.",
                    Data = result
                };

            }
            else
            {
                response = new HTTPResponseWrapper<LotoAE>()
                {
                    Status = Common.Constants.ShieldHttpWrapper.Status.NOT_MODIFIED,
                    Reason = Common.Constants.ShieldHttpWrapper.Reason.ALREADY_EXISTS,
                    Message = $"{request.AEName} cannot sign into this LOTO because they are signed into another LOTO",
                    Data = null
                };
            }

            return response;
        }

        public virtual List<LotoAE> GetAEsByLotoId(int lotoId)
        {
            return _lotoDao.GetAEsByLotoId(lotoId);
        }

        public virtual Models.Common.Status GetStatusByDescription(string description)
        {
            return _statusDao.GetStatusByDescription(description);
        }

        public virtual Models.Common.Status GetStatusById(int id)
        {
            return _statusDao.GetStatusById(id);
        }

        public virtual List<LotoAE> SignOutAE(int lotoId, int aeBemsId, string aeName, int userBemsId, string userName)
        {
            _lotoDao.SignOutAE(lotoId, aeBemsId);

            LotoDetails lotoToUpdate = GetLotoById(lotoId);

            if (lotoToUpdate != null)
            {
                Update(lotoToUpdate);
            }

            // AE signed themselves out
            if (aeBemsId == userBemsId)
            {
                _lotoTransactionService.AddTransaction(lotoId, $"{aeName} ({aeBemsId}) signed out as an AE");
            }
            // Manager signed out the AE
            else
            {
                _lotoTransactionService.AddTransaction(lotoId, $"{userName} ({userBemsId}) signed out {aeName} ({aeBemsId}) as an AE");
            }

            return _lotoDao.GetAEsByLotoId(lotoId);
        }

        public virtual List<LotoAE> SignOutVisitor(int lotoId, int id, string aeName, int userBemsId, string userName)
        {
            _lotoDao.SignOutVisitor(id);

            LotoDetails lotoToUpdate = GetLotoById(lotoId);

            if (lotoToUpdate != null)
            {
                Update(lotoToUpdate);
            }

            // Manager or GC signed out the Non Boeing nAE
            _lotoTransactionService.AddTransaction(lotoId, $"{userName} ({userBemsId}) signed out {aeName} as a Non Boeing AE");


            return _lotoDao.GetAEsByLotoId(lotoId);
        }

        /// <summary>
        /// The get airplane loto counts.
        /// </summary>
        /// <param name="aircraftList">
        /// The aircraft list.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary"/>.
        /// </returns>
        public virtual Dictionary<int, int> GetAirplaneLotoCounts(List<Shield.Services.Loto.Models.Common.Aircraft> aircraftList)
        {
            // Dictionary<Airplane Id, Loto Count>
            var returnDict = new Dictionary<int, int>();

            var modelList = aircraftList.Select(m => m.Model).ToList();
            var lineNumberList = aircraftList.Select(m => m.LineNumber).ToList();

            var lotos = _lotoDao.GetActiveLotosForAircrafts(modelList, lineNumberList);
            
            foreach (var aircraft in aircraftList)
            {
                var lotosForLineNumber = lotos.Where(loto => loto.LineNumber == aircraft.LineNumber).ToList();
                returnDict.Add(aircraft.Id, lotosForLineNumber.Count);
            }
            
            return returnDict;
        }

        public virtual List<LotoDetails> GetLotosWithActiveIsolationsByProgramAndLine(string program, string lineNumber)
        {
            return _lotoDao
                .GetIsolationsForLotos(program, lineNumber)
                .Where(loto => loto.Status.Description.ToLower() == "active" || loto.Status.Description.ToLower() == "transfer")
                .ToList();
        }

        private Discrete AssignedToLoto(Shield.Common.Models.Loto.StatusChangeRequest request)
        {
            Discrete discrete = _discreteService.GetDiscreteById(request.Id);

            if (discrete != null)
            {
                discrete.Status = GetStatusByDescription(Shield.Common.Constants.Status.ASSIGNED_TO_LOTO_DESCRIPTION);
                discrete = _discreteService.Update(discrete);
            }

            return discrete;
        }

        public virtual List<Shield.Services.Loto.Models.Hecp.HecpIsolationTag> GetConflictIsolations(int lotoId, string program, List<int> hecpIds, string lineNumber)
        {
            return _lotoDao.GetConflictIsolations(lotoId, program, hecpIds, lineNumber);
        }

        public virtual bool IsHecpDeletable(int hecpId)
        {
            return _lotoDao.IsHecpDeletable(hecpId);
        }

        public virtual LotoDetails UpdateLotoJobInfo(Models.Common.CreateLotoRequest lotoJobInfo)
        {
            _lotoTransactionService.AddTransaction(lotoJobInfo.LotoId, $"{lotoJobInfo.CreatedByName} ({lotoJobInfo.CreatedByBemsId}) Updated Work Package to: {lotoJobInfo.WorkPackage} and  Reason to: {lotoJobInfo.Reason}");

            return _lotoDao.UpdateLotoJobInfo(lotoJobInfo);
        }

    }
}
