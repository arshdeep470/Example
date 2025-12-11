using Newtonsoft.Json;
using Shield.Common.Models.MyLearning.Interfaces;
using Shield.Services.Loto.Models.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shield.Services.Loto.Models.Loto
{
    public class LotoDetails : Shield.Common.Models.Loto.Shared.Loto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int Id { get; set; }

        [JsonIgnore]
        public List<LotoTransaction> Transactions { get; set; }

        public List<LotoAE> ActiveAEs { get; set; }

        public List<Isolation> Isolations { get; set; }
        public List<Hecp.HecpIsolationTag> HecpIsolationTag { get; set; }

        [ForeignKey("StatusId")]
        public Status Status { get; set; }

        [ForeignKey("DiscreteId")]
        public Discrete.Discrete Discrete { get; set; }

        public int? HecpTableId { get; set; }

        public string Ata { get; set; }

        public List<LotoAssociatedHecps> LotoAssociatedHecps { get; set;}

        public List<LotoAssociatedModelData> LotoAssociatedModelDataList { get; set; } = new List<LotoAssociatedModelData>();

        [NotMapped]
        public List<IMyLearningDataResponse> UserTrainingData { get; set; }
    }
}
