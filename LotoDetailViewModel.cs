using Shield.Common.Models.Loto.Shared;
using Shield.Ui.App.ViewModels.Common;
using System.Collections.Generic;

namespace Shield.Ui.App.ViewModels
{
    public class LotoDetailViewModel : ShieldViewModel
    {
        public LotoViewModel Loto { get; set; }

        public AircraftHeaderViewModel Header { get; set; }

        public List<LotoTransaction> LotoTransactions { get; set; }

        public string SignInCardMessage { get; set; }
    }
}
