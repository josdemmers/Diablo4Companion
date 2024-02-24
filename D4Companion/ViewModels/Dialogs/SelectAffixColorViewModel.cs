using D4Companion.Entities;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class SelectAffixColorViewModel : BindableBase
    {
        private ObservableCollection<KeyValuePair<string, Color>> _colors = new();

        private ColorWrapper _color = new();
        private KeyValuePair<string, Color> _selectedColor = new KeyValuePair<string, Color>("Green", System.Windows.Media.Colors.Green);

        // Start of Constructors region

        #region Constructors

        public SelectAffixColorViewModel(Action<SelectAffixColorViewModel> closeHandler, ColorWrapper color)
        {
            // Init View commands
            CloseCommand = new DelegateCommand<SelectAffixColorViewModel>(closeHandler);
            SetAffixColorDoneCommand = new DelegateCommand(SetAffixColorDoneExecute);

            _color = color;

            // Init colors
            InitColors();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<KeyValuePair<string, Color>> Colors { get => _colors; set => _colors = value; }

        public DelegateCommand<SelectAffixColorViewModel> CloseCommand { get; }
        public DelegateCommand SetAffixColorDoneCommand { get; }

        public KeyValuePair<string, Color> SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                RaisePropertyChanged();

                _color.Color = _selectedColor.Value;
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void SetAffixColorDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitColors()
        {
            Colors.Clear();
            Colors.AddRange(GetColors());

            string colorName = _colors.FirstOrDefault(c => c.Value == _color.Color).Key ?? string.Empty;
            SelectedColor = new KeyValuePair<string, Color>(colorName, _color.Color);
        }

        private IEnumerable<KeyValuePair<string, Color>> GetColors()
        {
            return typeof(Colors)
                .GetProperties()
                .Where(prop =>
                    typeof(Color).IsAssignableFrom(prop.PropertyType))
                .Select(prop =>
                    new KeyValuePair<string, Color>(prop.Name, (Color)prop.GetValue(null)));
        }


        #endregion
    }
}
