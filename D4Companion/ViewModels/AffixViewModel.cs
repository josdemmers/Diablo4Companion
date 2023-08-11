using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.ViewModels
{
    public class AffixViewModel : BindableBase
    {
        private int? _badgeCount = null;

        // Start of Constructors region

        #region Constructors

        public AffixViewModel()
        {
            
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public int? BadgeCount { get => _badgeCount; set => _badgeCount = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }
}
