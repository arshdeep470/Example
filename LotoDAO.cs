using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using shield.services.loto.models.Loto;
using Shield.Common;
using Shield.Common.Models.Loto;
using Shield.Services.Loto.Models.Loto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shield.Services.Loto.Data.Impl
{
    using Shield.Services.Loto.Models.Hecp;

    public class LotoDAO : ILotoDAO
    {
        private LotoContext _context;
        private HttpClientService _clientService;

        public LotoDAO(LotoContext ctx, HttpClientService clientService)
        {
            _context = ctx;
            _clientService = clientService;
        }

        public LotoDetails GetLotoById(int lotoId)
        {
            LotoDetails loto = _context.LotoData.Include(l => l.Status)
                .Include(l => l.ActiveAEs)
                .Include(l => l.Discrete)
                .Include(l => l.LotoAssociatedHecps)
                .ThenInclude(l => l.LotoIsolationsDiscreteHecp)
                .Include(l => l.LotoAssociatedModelDataList)
                .AsSplitQuery()
                .FirstOrDefault(l => l.Id == lotoId);

            // Getting DiscreteIsolations from Isolations table
            loto.Isolations = _context.Isolations.Where(i => i.LotoId == loto.Id).ToList();

            // Conerting the Dicrete Isolation in the Isolation table to LotoIsolationsDiscreteHecps.
            List<LotoIsolationsDiscreteHecp> isolationDiscrete = new List<LotoIsolationsDiscreteHecp>();
            foreach (var iso in loto.Isolations)
            {
                isolationDiscrete.Add(new LotoIsolationsDiscreteHecp()
                {
                    CircuitNomenclature = iso.CircuitNomenclature,
                    InstallDateTime = iso.InstallDateTime,
                    InstalledByBemsId = iso.InstalledByBemsId,
                    IsLocked = iso.IsLocked,
                    RemovedByBemsId = iso.RemovedByBemsId,
                    RemovedDateTime = iso.RemovedDateTime,
                    SystemCircuitId = iso.SystemCircuitId,
                    Tag = iso.Tag
                });
            };

            // Manipulating LotoDetails to add the isolations to the new tables(LotoAssociatedHecps and LotoIsolationsDiscreteHecp)
            if (loto.Isolations.Count > 0 || ((loto.HECPTitle != null) ))
            {
                LotoDetails lotoWithDiscrete = new LotoDetails()
                {

                    ActiveAEs = loto.ActiveAEs,
                    Ata = null,
                    AssignedPAEBems = loto.AssignedPAEBems,
                    CreatedAt = loto.CreatedAt,
                    CreatedByBemsId = loto.CreatedByBemsId,
                    DateUpdated = loto.DateUpdated,
                    Discrete = null,
                    HECPId = null,
                    HecpIsolationTag = loto.HecpIsolationTag,
                    HECPRevisionLetter = null,
                    HecpTableId = null,
                    HECPTitle = null,
                    Isolations = null,
                    Id = loto.Id,
                    LineNumber = loto.LineNumber,
                    LotoAssociatedHecps = new List<LotoAssociatedHecps>()
                    {
                        new LotoAssociatedHecps()
                        {
                            Ata = loto.Ata,
                            HECPRevisionLetter = loto.HECPRevisionLetter,
                            HecpTableId = loto.HecpTableId,
                            HECPTitle = loto.HECPTitle,
                            Loto = loto,
                            LotoId = loto.Id,
                            LotoIsolationsDiscreteHecp = isolationDiscrete
                        }
                    },
                    Model = loto.Model,
                    PAESignInTime = loto.PAESignInTime,
                    Reason = loto.Reason,
                    Site = loto.Site,
                    Status = loto.Status,
                    Transactions = loto.Transactions,
                    WorkPackage = loto.WorkPackage,
                };
                // Removing Discrete Isolations from Isolation table
                _context.Isolations.RemoveRange(loto.Isolations);               
                return Update(lotoWithDiscrete);                
            }
            return loto;
        }

        public List<LotoDetails> GetLotos(string model, string lineNumber)
        {
            var lotosList = _context.LotoData.Where(loto => loto.Model == model && loto.LineNumber == lineNumber)
                .Include(l => l.ActiveAEs)
                .Include(st => st.Status)
                .Include(d => d.Discrete)
                .Include(m => m.LotoAssociatedModelDataList)
                .Include(h => h.LotoAssociatedHecps).ThenInclude(i=> i.LotoIsolationsDiscreteHecp)
                .Include(i => i.Isolations)
                .AsSplitQuery()
                .ToList();

            return lotosList;
        }


        public List<LotoDetails> GetIsolationsForLotos(string model, string lineNumber)
        {
            List<LotoDetails> lotosList = GetLotos(model, lineNumber)?.Where(
                loto => loto.Status?.Description.ToLower() == "active"
                        || loto.Status?.Description.ToLower() == "transfer").ToList();
            foreach (LotoDetails loto in lotosList)
            {
                if (loto.HECPTitle is not null && loto.Isolations.Count == 0)
                    loto.Isolations = _context.Isolations.Where(i => i.LotoId == loto.Id).ToList();
            }
            List<int> lotoIds = lotosList.Select(loto => loto.Id).ToList();
            List<HecpIsolationTag> hecpIsolationTags = _context.HecpIsolationTag.Where(i => lotoIds.Contains(i.LotoId)).ToList();

            foreach (LotoDetails loto in lotosList)
            {
                if (loto.HecpTableId is not null || loto.LotoAssociatedHecps.Any(x=> x.HecpTableId is not null))
                {
                    loto.HecpIsolationTag = hecpIsolationTags.Where(i => i.LotoId == loto.Id).ToList();
                }
            }
            return lotosList;
        }

        /// <summary>
        /// The get lotos for aircrafts.
        /// </summary>
        /// <param name="models">
        /// The models.
        /// </param>
        /// <param name="lineNumbers">
        /// The line numbers.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public List<LotoDetails> GetActiveLotosForAircrafts(List<string> models, List<string> lineNumbers)
        {
            var lotosList = _context.LotoData.Where(loto => models.Contains(loto.Model) && lineNumbers.Contains(loto.LineNumber))
                .Include(st => st.Status)
                .Where(l => l.Status.Description.ToLower().Equals("active"))
                .ToList();

            return lotosList;
        }

        public LotoDetails Create(LotoDetails loto)
        {
            DateTime now = DateTime.UtcNow;
            loto.CreatedAt = now;
            loto.DateUpdated = now;
            var result = _context.Add(loto).Entity;

            _context.SaveChanges();

            return result;
        }

        public LotoDetails Update(LotoDetails loto)
        {
            try
            {
                loto.DateUpdated = DateTime.UtcNow;
                var lotoDetails = _context.LotoData
                    .Where(l => l.Id == loto.Id)
                    .Include(l=> l.LotoAssociatedHecps)
                    .ThenInclude(x => x.LotoIsolationsDiscreteHecp)
                    .AsSplitQuery()
                    .FirstOrDefault();

                if (lotoDetails != null)
                {
                    lotoDetails.ActiveAEs = loto.ActiveAEs;
                    lotoDetails.Ata = loto.Ata;
                    lotoDetails.AssignedPAEBems = loto.AssignedPAEBems;
                    lotoDetails.CreatedAt = loto.CreatedAt;
                    lotoDetails.CreatedByBemsId = loto.CreatedByBemsId;
                    lotoDetails.DateUpdated = DateTime.UtcNow;
                    lotoDetails.Discrete = loto.Discrete;
                    lotoDetails.HECPId = loto.HECPId;
                    lotoDetails.HecpIsolationTag = loto.HecpIsolationTag;
                    lotoDetails.HECPRevisionLetter = loto.HECPRevisionLetter;
                    lotoDetails.HecpTableId = loto.HecpTableId;
                    lotoDetails.HECPTitle = loto.HECPTitle;
                    lotoDetails.Isolations = loto.Isolations;
                    lotoDetails.LineNumber = loto.LineNumber;
                    
                    if(loto.LotoAssociatedHecps is not null)
                    {
                        // Updating the LotoAsscoicated Hecps to retain Discrete Isolations
                        // and to add new Hecps and Discretes
                        foreach (var lotoAssociatedHecp in loto.LotoAssociatedHecps)
                        {
                            // Adding new Hecps and Discretes to the existing list by checking ID
                            if (lotoAssociatedHecp.Id == 0)
                            {
                                lotoDetails.LotoAssociatedHecps.Add(lotoAssociatedHecp);
                            }
                        }
                    }                   
                    //Removing Hecps and Dicretes which are removed in the UI
                    lotoDetails.LotoAssociatedHecps.RemoveAll(x => !loto.LotoAssociatedHecps.Select(a => a.Id).ToList().Contains(x.Id));
                    lotoDetails.Model = loto.Model;
                    lotoDetails.PAESignInTime = loto.PAESignInTime;
                    lotoDetails.Reason = loto.Reason;
                    lotoDetails.Status = loto.Status;
                    lotoDetails.Transactions = loto.Transactions;
                    lotoDetails.WorkPackage = loto.WorkPackage;
                }

                EntityEntry<LotoDetails> result = _context.Update(lotoDetails);
                _context.SaveChanges();
                LotoDetails entity = result.Entity;
                entity.Isolations = _context.Isolations.Where(isolation => isolation.LotoId == loto.Id).ToList();
                return entity;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return null;
            }
        }

        public void UpdateHecpIsolationtag(LotoDetails loto)
        {
            List<int> hecpIds = loto.LotoAssociatedHecps.Select(x => x.HecpTableId.GetValueOrDefault()).ToList();
            List<int> existingHecpIsolationTagHecpIds = _context.HecpIsolationTag.Where(i => i.LotoId == loto.Id).Select(x => x.HecpId).Distinct().ToList();
            foreach(var hecpId in existingHecpIsolationTagHecpIds)
            {
                if (!hecpIds.Contains(hecpId))
                {
                    List<HecpIsolationTag> hecpIsolationTagToRemove =_context.HecpIsolationTag.Where(i => i.LotoId == loto.Id && i.HecpId == hecpId).ToList();
                    _context.HecpIsolationTag.RemoveRange(hecpIsolationTagToRemove);
                    _context.SaveChanges();
                };
            };
        }

        public bool DeleteLoto(LotoDetails loto)
        {
            if (loto != null && _context.LotoData.Where(i => i.Id == loto.Id).FirstOrDefault() != null)
            {
                _context.LotoData.Remove(loto);
                List<HecpIsolationTag> hecpIsolationTags = _context.HecpIsolationTag.Where(i => i.LotoId == loto.Id).ToList();
                if(hecpIsolationTags.Count > 0)
                {
                    _context.HecpIsolationTag.RemoveRange(hecpIsolationTags);
                }
                _context.SaveChanges();
                return true;
            }

            return false;
        }

        public LotoAE SignInAE(LotoAE aESignIn)
        {
            try
            {
                aESignIn.Loto = GetLotoById(aESignIn.LotoId);
                aESignIn.Loto.DateUpdated = aESignIn.SignInTime;
                EntityEntry<LotoAE> result = _context.LotoAE.Add(aESignIn);

                _context.SaveChanges();

                LotoAE lotoAE = result.Entity;

                return lotoAE;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error in Loto DAO -- " + e);
                throw (e);
            }
        }

        public List<LotoAE> GetAEsByLotoId(int lotoId)
        {
            return _context.LotoAE.Where(l => l.LotoId == lotoId).AsNoTracking().ToList();
        }

        public void SignOutAE(int lotoId, int bemsId)
        {
            LotoAE toRemove = _context.LotoAE.Where(l => l.AEBemsId == bemsId && l.LotoId == lotoId).ToList().First();
            toRemove.Loto = _context.LotoData.Where(l => l.Id == lotoId).FirstOrDefault();
            Update(toRemove.Loto);
            _context.LotoAE.Remove(toRemove);

            _context.SaveChanges();
        }

        public void SignOutVisitor(int id)
        {
            LotoAE toRemove = _context.LotoAE.Where(l => l.Id == id).ToList().First();

            Update(toRemove.Loto);
            _context.LotoAE.Remove(toRemove);

            _context.SaveChanges();
        }

        public LotoDetails LotoAESignedInto(SigningAERequest signingAERequest)
        {
            var lineNumber = _context.LotoData.FirstOrDefault(x => x.Id == signingAERequest.LotoId).LineNumber;
            LotoDetails loto = _context.LotoData
                .Include(l => l.ActiveAEs)
                .FirstOrDefault(l => l.ActiveAEs.Where(ae => ae.AEBemsId == signingAERequest.AEBemsId
                && l.LineNumber != lineNumber).Count() >= 1);

            return loto;
        }

        public List<HecpIsolationTag> GetConflictIsolations(int lotoId, string program, List<int> hecpIds, string lineNumber)
        {
            var hecpIsolationToBeLocked = _context.HecpIsolationTag.Where(l => l.LotoId == lotoId && hecpIds.Contains(l.HecpId) && l.IsLocked == true).ToList();
            List<HecpIsolationTag> conflictedIsolations = new List<HecpIsolationTag>();
            List<HecpIsolationTag> conflictIsolationList = new List<HecpIsolationTag>();
            int i = 1;
            int cntRow = 0;
            var lotoName = string.Empty;
            foreach (HecpIsolationTag iso in hecpIsolationToBeLocked)
            {
                if (!string.IsNullOrEmpty(iso.CircuitId))
                { conflictIsolationList = _context.HecpIsolationTag.Where(l => l.LotoId != lotoId && ((string.IsNullOrEmpty(l.CircuitId) ? "" : l.CircuitId.Trim().ToUpper()) == iso.CircuitId.Trim().ToUpper()) && l.State != iso.State && l.IsLocked == true).ToList(); }
                else if (!string.IsNullOrEmpty(iso.CircuitName))
                { 
                    conflictIsolationList = _context.HecpIsolationTag.Where(l => l.LotoId != lotoId && ((string.IsNullOrEmpty(l.CircuitName) ? "" : l.CircuitName.Trim().ToUpper()) == iso.CircuitName.Trim().ToUpper()) && l.State != iso.State && l.IsLocked == true).ToList(); 
                }
                cntRow = 0;
                foreach (HecpIsolationTag conflictIsolation in conflictIsolationList)
                {    
                    if (cntRow == 0)
                    {
                        var lotosList = _context.LotoData.Where(loto => loto.Id == conflictIsolation.LotoId && loto.Model == program && loto.LineNumber.ToUpper().Equals(lineNumber.ToUpper())).Include(st => st.Status).ToList();
                        var lotoActive = lotosList.Where(l => (l.Status.Description.ToUpper() == ShieldConstants.Constants.STATUS_LOTO_ACTIVE || l.Status.Description.ToUpper() == ShieldConstants.Constants.STATUS_LOTO_TRANSFER)).FirstOrDefault();
                        if(lotoActive != null)
                        {
                            if (!string.IsNullOrEmpty(lotoName))
                            {
                                if (!lotoName.Equals(lotoActive.WorkPackage)) { i = i + 1; }
                            }
                            lotoName = lotoActive.WorkPackage;
                            conflictIsolation.CircuitLocation = lotoActive.WorkPackage;
                            conflictIsolation.LotoId = i;
                            conflictedIsolations.Add(conflictIsolation);
                            cntRow = cntRow + 1;
                        }
                    }
                   
                }                
            }
            return conflictedIsolations;
        }

        public bool IsHecpDeletable(int hecpId)
        {
            return _context.LotoData
                            .Include(i => i.Status)
                            .Include(h => h.LotoAssociatedHecps)
                            .Where(l => l.Status.Description != "Completed" && (l.HecpTableId == hecpId || 
                            l.LotoAssociatedHecps.Select(i => i.HecpTableId).Contains(hecpId))).Count() == 0;
        }

        #region Visitor Check In
        public LotoAE SignInVisitor(LotoAE aESignIn)
        {
            try
            {
                aESignIn.Loto = GetLotoById(aESignIn.LotoId);
                aESignIn.Loto.DateUpdated = aESignIn.SignInTime;
                EntityEntry<LotoAE> result = _context.LotoAE.Add(aESignIn);

                _context.SaveChanges();

                LotoAE lotoAE = result.Entity;

                return lotoAE;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error in Loto DAO -- " + e);
                throw (e);
            }
        }

        public LotoAE LotoVisitorSignedInto(SigningAERequest signingAERequest)
        {
            LotoAE lotoAE = _context.LotoAE.Where(l => l.FullName == signingAERequest.AEName).FirstOrDefault();

            return lotoAE;
        }

        public List<LotoAE> GetVisitorsByLotoId(int lotoId)
        {
            return _context.LotoAE.Where(l => l.LotoId == lotoId).AsNoTracking().ToList();
        }

        public void SignOutVisitor(int lotoId, int bemsId)
        {
            LotoAE toRemove = _context.LotoAE.Where(l => l.AEBemsId == bemsId && l.LotoId == lotoId).ToList().First();

            Update(toRemove.Loto);
            _context.LotoAE.Remove(toRemove);

            _context.SaveChanges();
        }
        #endregion

        public LotoDetails UpdateLotoJobInfo(Models.Common.CreateLotoRequest lotoJobInfo)
        {
            LotoDetails lotoDetails = _context.LotoData.Where(x => x.Id == lotoJobInfo.LotoId).FirstOrDefault();
            lotoDetails.WorkPackage = lotoJobInfo.WorkPackage;
            lotoDetails.Reason = lotoJobInfo.Reason;
            _context.SaveChanges();
            return lotoDetails;
        }
    }
}
