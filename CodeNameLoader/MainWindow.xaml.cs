using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CodeNameLoader.API.Core;
using CodeNameLoader.API.Requests;
using CodeNameLoader.API.Responses;

namespace CodeNameLoader;

public partial class MainWindow : Window
{
    private Page _currentPage;
    private Page CurrentPage => _currentPage;


    public MainWindow()
    {
        InitializeComponent();

        _currentPage = new RegisterPage();
        this.AddChild(_currentPage);
    }
}
