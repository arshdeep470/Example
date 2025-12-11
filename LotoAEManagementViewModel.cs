using Shield.Common.Models.MyLearning;
using Shield.Ui.App.ViewModels.Common;
using System.Collections.Generic;

namespace Shield.Ui.App.ViewModels
{
    public class LotoAEManagementViewModel : ShieldViewModel
    {
        public AircraftHeaderViewModel Header { get; set; }

        public LotoViewModel Loto { get; set; }

        public int BemsId { get; set; }
        public string Message { get; set; }
        public string AEName { get; set; }
        public bool IsTrainingDown { get; set; }
        public int LotoId { get; set; }
        public List<MyLearningDataResponse> UserTrainingData { get; set; }
    }
}
