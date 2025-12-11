using Shield.Ui.App.Models.CommonModels;
using Shield.Ui.App.Models.DiscreteModels;
using Shield.Ui.App.Models.HecpModels;
using Shield.Ui.App.Models.LotoModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Shield.Ui.App.ViewModels
{
    /// <summary>
    /// The loto view model.
    /// </summary>
    public class LotoViewModel : LotoWithStatus
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the program.
        /// </summary>
        public string Program { get; set; }

        /// <summary>
        /// Gets or sets the line number.
        /// </summary>
        public string LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the the aircraft gc bems.
        /// </summary>
        public int TheAircraftGCBems { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the hecp title.
        /// </summary>
        [Display(Name = "HECP Title")]
        public string HecpTitle { get; set; }

        /// <summary>
        /// Gets or sets the hecp id.
        /// </summary>
        [Display(Name = "HECP Id")]
        public string HecpId { get; set; }

        /// <summary>
        /// Gets or sets the hecp revision letter.
        /// </summary>
        [Display(Name = "HECP Revision Letter")]
        public string HECPRevisionLetter { get; set; }

        /// <summary>
        /// Gets or sets the reason.
        /// </summary>
        [Display(Name = "HECP Reason")]
        [Required]
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the work package.
        /// </summary>
        [Display(Name = "HECP Work Package")]
        [Required]
        public string WorkPackage { get; set; }

        /// <summary>
        /// Gets or sets the hecp table id.
        /// </summary>
        public int? HecpTableId { get; set; }

        /// <summary>
        /// Gets or sets the ata.
        /// </summary>
        public string Ata { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the assigned pae.
        /// </summary>
        public User AssignedPAE { get; set; }

        /// <summary>
        /// Gets or sets the assigned pae bems id.
        /// </summary>
        public int? AssignedPAEBemsId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is current user the assigned pae.
        /// </summary>
        public bool IsCurrentUserTheAssignedPAE { get; set; }

        /// <summary>
        /// Gets or sets the assigned a es.
        /// </summary>
        public List<AssignedAEViewModel> AssignedAEs { get; set; } = new();

        /// <summary>
        /// Gets or sets the isolations.
        /// </summary>
        public List<IsolationViewModel> Isolations { get; set; } = new();

        /// <summary>
        /// Gets or sets the hecp isolation tags.
        /// </summary>
        public List<HecpIsolationTag> HecpIsolationTags { get; set; } = new();

        /// <summary>
        /// Gets or sets the conflict isolations.
        /// </summary>
        public List<ConflictIsolationViewModel> ConflictIsolations { get; set; } = new();

        /// <summary>
        /// Gets a value indicating whether is active ae.
        /// </summary>
        public bool IsActiveAE { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether show ae sign out warning.
        /// </summary>
        public bool ShowAESignOutWarning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is assigned pae exists.
        /// </summary>
        public bool IsAssignedPAEExists { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether has valid job info data.
        /// </summary>
        public bool HasValidJobInfoData { get; set; }

        /// <summary>
        /// Gets a value indicating whether was save loto successful.
        /// </summary>
        public bool WasSaveLotoSuccessful { get; internal set; }

        /// <summary>
        /// Gets or sets the discrete.
        /// </summary>
        public Discrete Discrete { get; set; }

        /// <summary>
        /// Gets or sets the loto associated hecps.
        /// </summary>
        public List<LotoAssociatedHecps> LotoAssociatedHecps { get; set; }

        /// <summary>
        /// Gets or sets the loto associated model data list.
        /// </summary>
        public List<LotoAssociatedModelData> LotoAssociatedModelDataList { get; set; } = new();

        /// <summary>
        /// Gets or sets the minor model display value.
        /// </summary>
        public string MinorModelDisplayValue { get; set; }

        /// <summary>
        /// Gets or sets the loto users.
        /// </summary>
        public List<User> LotoUsers { get; set; }

        /// <summary>
        /// The get active ae count.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int GetActiveAECount()
        {
            return this.AssignedAEs.Count;
        }

        /// <summary>
        /// The show transfer.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool ShowTransfer()
        {
            return this.AssignedAEs.Count == 0 && this.IsActive();
        }

        /// <summary>
        /// The get minor model id list.
        /// </summary>
        /// <returns>
        /// The <see cref="List{Int}"/>.
        /// </returns>
        public List<int?> GetMinorModelIdList()
        {
            return this.LotoAssociatedModelDataList?.Where(x => x.MinorModelId != null).Select(x => x.MinorModelId).ToList();
        }

        public string MinorModelIdList { get; set; }

        /// <summary>
        /// The show complete.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool ShowComplete()
        {
            if (this.IsActive())
            {
                var areNotLockedIsolations = this.Isolations.Any(prop => prop.IsLocked == false);
                if (this.Discrete != null)
                {
                    areNotLockedIsolations = this.Discrete.DiscreteDeactivationSteps
                        .Where(step => step.Isolations?.Count > 0)
                        .SelectMany(step => step.Isolations)
                        .Any(prop => prop.IsLocked == false);
                }

                if (areNotLockedIsolations)
                {
                    return false;
                }

                if (this.AssignedAEs.Count == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public string getMinorModelDisplayTextforIsolation()
        {
            StringBuilder displayText = new("Note : All Isolations below are for ");
            if (LotoAssociatedModelDataList.Count == 1 && LotoAssociatedModelDataList[0].MinorModelId != null)
            {
                displayText.Append(MinorModelDisplayValue);
            }
            else
            {
                displayText.Append(Program);
            }

            return displayText.ToString();
        }
    }
}
