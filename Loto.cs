using Shield.Common.Models.Loto.Shared;
using Shield.Ui.App.Models.HecpModels;
using System;
using System.Collections.Generic;

namespace Shield.Ui.App.Models.LotoModels
{
    using Shield.Common.Models.MyLearning;
    using Shield.Ui.App.Models.DiscreteModels;

    /// <summary>
    /// The loto.
    /// </summary>
    public class Loto : Shield.Common.Models.Loto.Shared.Loto
    {
        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Gets or sets the isolations.
        /// </summary>
        public List<Isolation> Isolations { get; set; } = new();

        /// <summary>
        /// Gets or sets the hecp isolation tag.
        /// </summary>
        public List<HecpIsolationTag> HecpIsolationTag { get; set; } = new();

        /// <summary>
        /// Gets or sets the active a es.
        /// </summary>
        public List<AssignedAE> ActiveAEs { get; set; } = new();

        /// <summary>
        /// Gets or sets the discrete.
        /// </summary>
        public Discrete Discrete { get; set; }

        /// <summary>
        /// Gets or sets the hecp.
        /// </summary>
        public Hecp Hecp { get; set; }

        /// <summary>
        /// Gets or sets the hecp table id.
        /// </summary>
        public int? HecpTableId { get; set; }

        /// <summary>
        /// Gets or sets the ata.
        /// </summary>
        public string Ata { get; set; }

        /// <summary>
        /// Gets or sets the loto associated hecps.
        /// </summary>
        public List<LotoAssociatedHecps> LotoAssociatedHecps { get; set; }

        /// <summary>
        /// Gets or sets the loto associated model data list.
        /// </summary>
        public List<LotoAssociatedModelData> LotoAssociatedModelDataList { get; set; } = new();

        /// <summary>
        /// Gets or sets the training status.
        /// </summary>
        public List<MyLearningDataResponse> UserTrainingData { get; set; }
    }
}
