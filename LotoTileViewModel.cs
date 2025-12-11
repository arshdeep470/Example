using Shield.Ui.App.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace Shield.Ui.App.ViewModels
{
    /// <summary>
    /// The loto tile view model.
    /// </summary>
    public class LotoTileViewModel : LotoWithStatus
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the hecp revision letter.
        /// </summary>
        public string HecpRevisionLetter { get; set; }

        /// <summary>
        /// Gets or sets the reason.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the subtitle.
        /// </summary>
        public List<string> Subtitle { get; set; } = new();

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date updated.
        /// </summary>
        public DateTime DateUpdated { get; set; }

        /// <summary>
        /// Gets or sets the assigned pae.
        /// </summary>
        public User AssignedPAE { get; set; }

        /// <summary>
        /// Gets or sets the active ae count.
        /// </summary>
        public int ActiveAECount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether show ae sign out warning.
        /// </summary>
        public bool ShowAESignOutWarning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether show discrete tag.
        /// </summary>
        public bool ShowDiscreteTag { get; set; }

        /// <summary>
        /// Gets or sets the minor model display value.
        /// </summary>
        public string MinorModelDisplayValue { get; set; }

        /// <summary>
        /// Gets or sets the program.
        /// </summary>
        public string Program { get; set; }
    }
}
