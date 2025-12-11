using Shield.Ui.App.Models.CommonModels;
using System;

namespace Shield.Ui.App.ViewModels
{
    public class AssignedAEViewModel
    {
        public int Id { get; set; }

        public int LotoId { get; set; }

        public User AssignedAE { get; set; }

        public DateTime SignInTime { get; set; }

        public bool ShowAESignOutWarning { get; set; }

        public bool IsCurrentUser { get; set; }
    }
}