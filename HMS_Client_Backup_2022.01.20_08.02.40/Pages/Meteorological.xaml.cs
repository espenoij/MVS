﻿using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for NOROG_Meteorological.xaml
    /// </summary>
    public partial class NOROG_Meteorological : UserControl
    {
        public NOROG_Meteorological()
        {
            InitializeComponent();
        }

        public void Init(MeteorologicalVM viewModel)
        {
            DataContext = viewModel;
        }
    }
}