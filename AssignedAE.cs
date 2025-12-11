using Shield.Common.Models.MyLearning;
using System;
using System.Collections.Generic;

namespace Shield.Ui.App.Models.LotoModels
{
    public class AssignedAE
    {
        public int Id { get; set; }

        public int LotoId { get; set; }

        public string AEName { get; set; }

        public int AEBemsId { get; set; }

        public DateTime SignInTime { get; set; }

        public string FullName { get; set; }
        public List<MyLearningDataResponse> UserTrainingData { get; set; }
    }

}
