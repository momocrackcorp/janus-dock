using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace JanusDock;

public sealed class DockOptionsForm : Form
{
    public string Language { get; private set; } = "es";
    public bool AlwaysOnTop { get; private set; }
    public bool AutoHide { get; private set; }
    public bool ShowNames { get; private set; }
    public string NameMode { get; private set; } = "Tooltip";
    public string DockMode { get; private set; } = "Acoplada";
    public bool ShowDiskWidget { get; private set; }
    public bool ShowNetworkTraffic { get; private set; }
    public bool ShowSystemUsage { get; private set; }
    public bool ShowCpu { get; private set; } = true;
    public bool ShowRam { get; private set; } = true;
    public bool NetworkAtEnd { get; private set; }
    public bool StartWithWindows { get; private set; }
    public int DiskColorArgb { get; private set; }
    public Dictionary<string,int> DiskColors { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> HiddenDiskLetters { get; private set; } = new();
    public bool HoverAnimation { get; private set; }
    public string HoverEffect { get; private set; } = "Lupa";
    public int ZoomPercent { get; private set; } = 280;
    public bool ShowLock { get; private set; }
    public bool ShowShutdown { get; private set; }
    public bool ShowRestart { get; private set; }
    public int CriticalPercent { get; private set; }
    public int IconSize { get; private set; }
    public int IconSpacing { get; private set; }
    public int BarWidth { get; private set; }
    public int BarHeight { get; private set; }
    public string IconStyle { get; private set; } = "Windows";
    public string BarColorMode { get; private set; } = "Windows";
    public int CustomBarColorArgb { get; private set; } = System.Drawing.Color.FromArgb(36, 57, 64).ToArgb();
    public List<DockItem> OrderedItems { get; private set; } = new();
    public string Edge { get; private set; } = "Derecha";
    const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "JANUS Dock";

    public event Action<DockOptionsForm>? ApplyRequested;
    private Action? liveChange;
    private void ScheduleLiveChange() => liveChange?.Invoke();
    public DockOptionsForm(DockSettings config, string? initialTab = null)
    {
        Text = "Opciones de JANUS Dock";
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1080, 720);
        MinimumSize = new Size(790, 550);
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9);
        StyleWindowsControls(this);

        // Fluent-style navigation: real pages, no legacy tab headers.
        ClientSize = new Size(1080, 720);
        MinimumSize = new Size(790, 550);
        BackColor = Color.FromArgb(229, 236, 244);
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill, BackColor = BackColor,
            ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(root);
        var sidebar = new Panel {
            Name = "SettingsSidebar", Dock = DockStyle.Fill, Width = 190,
            BackColor = Color.FromArgb(210, 224, 238)
        };
        var pageHost = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        root.Controls.Add(sidebar, 0, 0);
        root.Controls.Add(pageHost, 1, 0);
        var general = new Panel { Name = "General", Dock = DockStyle.Fill, BackColor = BackColor };
        var appearance = new Panel { Name = "Apariencia", Dock = DockStyle.Fill, BackColor = BackColor };
        var accessPage = new Panel { Name = "Accesos", Dock = DockStyle.Fill, BackColor = BackColor };
        var systemPage = new Panel { Name = "Sistema", Dock = DockStyle.Fill, BackColor = BackColor };
        var about = new Panel { Name = "Acerca de", Dock = DockStyle.Fill, BackColor = BackColor };
        var pages = new[] { general, appearance, accessPage, systemPage, about };
        foreach (var page in pages) { pageHost.Controls.Add(page); page.Visible = false; }
        sidebar.Controls.Add(new Label {
            Text = "JANUS Dock", Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(27, 75, 109), Location = new Point(22, 18),
            AutoSize = true
        });
        sidebar.Controls.Add(new Label {
            Text = "Configuración", Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(91, 111, 133), Location = new Point(23, 49),
            AutoSize = true
        });
        var navigation = new FlowLayoutPanel {
            Name = "SettingsNavigation", Location = new Point(8, 93), Width = 174,
            Height = 330, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = false, BackColor = sidebar.BackColor
        };
        sidebar.Controls.Add(navigation);
        string[] navNames = { "General", "Apariencia", "Accesos", "Sistema", "Acerca de" };
        string[] navSymbols = { "⚙", "◈", "▦", "▣", "ⓘ" };
        var navButtons = new List<SettingsNavButton>();
        void ShowPage(int selected)
        {
            for (int index = 0; index < pages.Length; index++)
            {
                pages[index].Visible = index == selected;
                var button = navButtons[index];
                button.SelectedPage = index == selected;
                button.BackColor = sidebar.BackColor;
                button.Invalidate();
                button.ForeColor = index == selected ? Color.FromArgb(11, 103, 188) : Color.FromArgb(40, 58, 78);
                button.Font = new Font("Segoe UI", 10, index == selected ? FontStyle.Bold : FontStyle.Regular);
            }
            pages[selected].BringToFront();
            if (pages[selected].Tag is FlowLayoutPanel activeRows) FitRows(activeRows);
        }
        for (int index = 0; index < navNames.Length; index++)
        {
            int pageNumber = index;
            var button = new SettingsNavButton {
                Text = $"{navSymbols[index]}   {navNames[index]}",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f), Width = 170, Height = 46,
                Margin = new Padding(2, 0, 2, 5),
                BackColor = sidebar.BackColor, ForeColor = Color.FromArgb(40, 58, 78),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => ShowPage(pageNumber);
            navigation.Controls.Add(button);
            navButtons.Add(button);
        }
        ShowPage(initialTab == "Apariencia" ? 1 : initialTab == "Acerca de" ? 4 : 0);
        appearance.Controls.Add(new Label { Text = "Estilo de los iconos:", Location = new Point(22, 32), AutoSize = true });
        var iconStyle = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(175, 28), Width = 260 };
        iconStyle.Items.AddRange(new object[] { "Iconos de Windows", "Iconos Lucide" });
        iconStyle.SelectedIndex = config.IconStyle == "Lucide" ? 1 : 0;
        appearance.Controls.Add(iconStyle);
        appearance.Controls.Add(new Label { Text = "Windows: usa los iconos originales del sistema y de los accesos.", Location = new Point(22, 83), AutoSize = true });
        appearance.Controls.Add(new Label { Text = "Lucide: estilo de líneas para Mi PC, Documentos, Descargas,", Location = new Point(22, 113), AutoSize = true });
        appearance.Controls.Add(new Label { Text = "carpetas, Apagar y Reiniciar. Los programas conservan su icono.", Location = new Point(22, 135), AutoSize = true });
        appearance.Controls.Add(new Label { Text = "Organizar accesos y separadores", Location = new Point(22, 177), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        appearance.Controls.Add(new Label { Text = "Arrastra filas para ordenarlas o utiliza los botones. Guardar aplica los cambios.", Location = new Point(22, 200), AutoSize = true });
        var ordered = config.Items.Select(i => new DockItem { Name = i.Name, Path = i.Path, IsSeparator = i.IsSeparator, IsDiskWidget = i.IsDiskWidget }).ToList();
        var history = new Stack<List<DockItem>>();
        var orderList = new ListBox { Location = new Point(22, 226), Size = new Size(365, 130), AllowDrop = true, IntegralHeight = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        string LabelFor(DockItem i) => i.IsSeparator ? "──────── Separador ────────" : i.IsDiskWidget ? "▣ Indicadores de discos" : i.Path == "janus:shutdown" ? "⏻ Apagar" : i.Path == "janus:restart" ? "↻ Reiniciar" : (string.IsNullOrWhiteSpace(i.Name) ? i.Path : i.Name);
        void RefreshOrder(int selected)
        {
            orderList.BeginUpdate();
            orderList.Items.Clear();
            foreach (var entry in ordered) orderList.Items.Add(LabelFor(entry));
            orderList.EndUpdate();
            if (ordered.Count > 0) orderList.SelectedIndex = Math.Clamp(selected, 0, ordered.Count - 1);
        }
        void Snapshot() => history.Push(new List<DockItem>(ordered));
        void MoveSelected(int delta)
        {
            int from = orderList.SelectedIndex, to = from + delta;
            if (from < 0 || to < 0 || to >= ordered.Count) return;
            Snapshot();
            var element = ordered[from]; ordered.RemoveAt(from); ordered.Insert(to, element);
            RefreshOrder(to);
        }
        orderList.MouseDown += (_, e) =>
        {
            int from = orderList.IndexFromPoint(e.Location);
            if (from >= 0 && from < ordered.Count)
            {
                orderList.SelectedIndex = from;
                orderList.DoDragDrop(from, DragDropEffects.Move);
            }
        };
        orderList.DragOver += (_, e) => { e.Effect = e.Data?.GetDataPresent(typeof(int)) == true ? DragDropEffects.Move : DragDropEffects.None; };
        orderList.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(typeof(int)) is not int from || from < 0 || from >= ordered.Count) return;
            var point = orderList.PointToClient(new Point(e.X, e.Y));
            int to = orderList.IndexFromPoint(point);
            if (to == ListBox.NoMatches) to = ordered.Count - 1;
            if (to == from) return;
            Snapshot();
            var element = ordered[from]; ordered.RemoveAt(from); ordered.Insert(to, element);
            RefreshOrder(to);
        };
        appearance.Controls.Add(orderList);
        var actions = new FlowLayoutPanel { Location = new Point(395, 226), Size = new Size(128, 140), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        Button Action(string label)
        {
            var button = new Button { Text = label, Width = 110, Height = 26, Margin = new Padding(1) };
            actions.Controls.Add(button);
            return button;
        }
        Action("↑ Subir").Click += (_, _) => MoveSelected(-1);
        Action("↓ Bajar").Click += (_, _) => MoveSelected(1);
        Action("+ Separador").Click += (_, _) =>
        {
            Snapshot();
            int index = Math.Clamp(orderList.SelectedIndex + 1, 0, ordered.Count);
            ordered.Insert(index, new DockItem { Name = "Separador", IsSeparator = true });
            RefreshOrder(index);
        };
        Action("Eliminar").Click += (_, _) =>
        {
            int index = orderList.SelectedIndex;
            if (index < 0) return;
            Snapshot();
            ordered.RemoveAt(index);
            RefreshOrder(index);
        };
        Action("↶ Deshacer").Click += (_, _) =>
        {
            if (history.Count == 0) return;
            ordered = history.Pop();
            RefreshOrder(0);
        };
        RefreshOrder(0);
        // Organizing remains in Apariencia; use stacked rows on narrow/high-DPI displays.
        appearance.Controls.Clear();
        var appearanceRows = PageRows(appearance);
        appearanceRows.Controls.Add(SectionTitle("Iconos", "Ajusta el tamaño y la separación de los accesos."));
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Estilo de los iconos:", AutoSize = true }, iconStyle));
        var barColorMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        barColorMode.Items.AddRange(new object[] { "Tema de Windows", "Color personalizado", "Clásico (Windows 95/98/2000/ME)" });
        barColorMode.SelectedIndex = config.BarColorMode == "Custom" ? 1 : config.BarColorMode == "Classic" ? 2 : 0;
        int selectedBarArgb = config.CustomBarColorArgb;
        var chooseBarColor = new Button { Text = "Elegir color...", AutoSize = true, MinimumSize = new Size(125, 36), BackColor = Color.FromArgb(selectedBarArgb) };
        chooseBarColor.ForeColor = chooseBarColor.BackColor.GetBrightness() > .55f ? Color.Black : Color.White;
        chooseBarColor.Click += (_, _) =>
        {
            using var dialog = new ColorDialog { FullOpen = true, Color = Color.FromArgb(selectedBarArgb) };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            selectedBarArgb = dialog.Color.ToArgb();
            chooseBarColor.BackColor = dialog.Color;
            chooseBarColor.ForeColor = dialog.Color.GetBrightness() > .55f ? Color.Black : Color.White;
        };
        chooseBarColor.Enabled = barColorMode.SelectedIndex == 1;
        barColorMode.SelectedIndexChanged += (_, _) => chooseBarColor.Enabled = barColorMode.SelectedIndex == 1;


        // Icon size belongs with all other appearance settings.
        var size = new NumericUpDown { Minimum = 16, Maximum = 96, Increment = 8, Value = Math.Clamp(config.IconSize, 16, 96), Width = 95 };
        var smallIcons = new CheckBox { Text = "Iconos pequeños (24 px)", AutoSize = true, Checked = config.IconSize <= 24 };
        smallIcons.CheckedChanged += (_, _) => { if (smallIcons.Checked) size.Value = 24; else if (size.Value == 24) size.Value = 48; };
        var iconSlider = new TrackBar { Minimum = 16, Maximum = 96, TickFrequency = 8, SmallChange = 1, LargeChange = 8, Value = (int)size.Value, Width = 280, Height = 48, AutoSize = false };
        var iconSizeLabel = new Label { Text = $"{size.Value} px", AutoSize = true, ForeColor = Color.FromArgb(16, 157, 178), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        iconSlider.ValueChanged += (_, _) => { size.Value = iconSlider.Value; iconSizeLabel.Text = $"{iconSlider.Value} px"; };
        size.ValueChanged += (_, _) => { if (iconSlider.Value != (int)size.Value) iconSlider.Value = (int)size.Value; };
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Tamaño de iconos:", AutoSize = true }, iconSlider, iconSizeLabel));
        appearanceRows.Controls.Add(smallIcons);
        var spacingSlider = new TrackBar { Minimum = 0, Maximum = 24, TickFrequency = 4, SmallChange = 1, LargeChange = 2, Width = 270, Value = Math.Clamp(config.IconSpacing, 0, 24) };
        var spacingLabel = new Label { Text = $"{spacingSlider.Value} px", AutoSize = true };
        spacingSlider.ValueChanged += (_, _) => spacingLabel.Text = $"{spacingSlider.Value} px";
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Separación entre iconos:", AutoSize = true }, spacingSlider, spacingLabel));

        var barWidth = new NumericUpDown { Minimum = 64, Maximum = 300, Increment = 4, Width = 100, Value = Math.Clamp(config.Width, 64, 300) };
        var barHeight = new NumericUpDown { Minimum = 48, Maximum = 160, Increment = 4, Width = 100, Value = Math.Clamp(config.HorizontalHeight, 48, 160) };
        var animation = new CheckBox { Text = "Activar efecto lupa al pasar el mouse", AutoSize = true, Checked = config.HoverAnimation };
        var zoomChoice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
        zoomChoice.Items.AddRange(new object[] { "150 %", "180 %", "205 %", "240 %", "280 %", "340 %" });
        int[] zoomValues = { 150, 180, 205, 240, 280, 340 };
        int chosenZoom = Math.Clamp(config.ZoomPercent, 150, 340);
        zoomChoice.SelectedIndex = Array.IndexOf(zoomValues, chosenZoom);
        if (zoomChoice.SelectedIndex < 0) zoomChoice.SelectedIndex = 5;
        zoomChoice.Enabled = animation.Checked;
        animation.CheckedChanged += (_, _) => zoomChoice.Enabled = animation.Checked;
        appearanceRows.Controls.Add(SectionTitle("Barra", "Color y dimensiones según su orientación."));
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Color de la barra:", AutoSize = true }, barColorMode, chooseBarColor));
        var widthTrack = new TrackBar { Minimum = 64, Maximum = 300, TickFrequency = 32,
            SmallChange = 2, LargeChange = 8, Width = 280, Value = (int)barWidth.Value };
        var heightTrack = new TrackBar { Minimum = 48, Maximum = 160, TickFrequency = 16,
            SmallChange = 2, LargeChange = 8, Width = 280, Value = (int)barHeight.Value };
        var widthValue = new Label { Text = $"{widthTrack.Value} px", AutoSize = true };
        var heightValue = new Label { Text = $"{heightTrack.Value} px", AutoSize = true };
        widthTrack.ValueChanged += (_, _) => {
            barWidth.Value = widthTrack.Value;
            widthValue.Text = $"{widthTrack.Value} px";
        };
        heightTrack.ValueChanged += (_, _) => {
            barHeight.Value = heightTrack.Value;
            heightValue.Text = $"{heightTrack.Value} px";
        };
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Ancho barra vertical:", AutoSize = true }, widthTrack, widthValue));
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Alto barra horizontal:", AutoSize = true }, heightTrack, heightValue));
        var hoverEffect = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
        hoverEffect.Items.AddRange(new object[] { "Ninguno", "Foco", "Lupa" });
        hoverEffect.SelectedIndex = config.HoverEffect == "Foco" ? 1 : config.HoverEffect == "Ninguno" ? 0 : config.HoverAnimation ? 2 : 1;
        hoverEffect.SelectedIndexChanged += (_, _) => zoomChoice.Enabled = hoverEffect.SelectedIndex == 2;
        zoomChoice.Enabled = hoverEffect.SelectedIndex == 2;
        appearanceRows.Controls.Add(SectionTitle("Efectos", "Elige cómo reaccionan los accesos."));
        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Efecto al pasar el mouse:", AutoSize = true }, hoverEffect));

        appearanceRows.Controls.Add(FieldRow(new Label { Text = "Ampliación de la lupa:", AutoSize = true }, zoomChoice));


        var accessesSection = SectionTitle("Organizar accesos", "Arrastra o utiliza los controles para ordenar.");
        appearanceRows.Controls.Add(accessesSection);
        orderList.Height = 170;
        appearanceRows.Controls.Add(orderList);
        actions.FlowDirection = FlowDirection.LeftToRight;
        actions.WrapContents = true;
        actions.AutoScroll = false;
        actions.AutoSize = false;
        actions.AutoScroll = false;
        actions.WrapContents = true;
        foreach (Control control in actions.Controls) { control.Width = Math.Max(116, control.PreferredSize.Width + 16); control.Height = Math.Max(34, control.PreferredSize.Height + 7); }
        appearanceRows.Controls.Add(actions);
        appearanceRows.SizeChanged += (_, _) => FitRows(appearanceRows);


        var top = new CheckBox { Text = "Mantener siempre visible", Checked = config.AlwaysOnTop, AutoSize = true, Location = new Point(18, 22) };
        var hide = new CheckBox { Text = "Ocultar automáticamente", Checked = config.AutoHide, AutoSize = true, Location = new Point(18, 54) };
        var startup = new CheckBox { Text = "Iniciar JANUS Dock con Windows", Checked = IsStartupEnabled(), AutoSize = true, Location = new Point(18, 86) };
        var names = new Label { Text = "Nombres de accesos:", AutoSize = true, Location = new Point(18, 115) };
        var nameMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(190, 110), Width = 205 };
        nameMode.Items.AddRange(new object[] { "Sin texto", "Mostrar al pasar el mouse" });
        nameMode.SelectedIndex = config.NameMode == "None" ? 0 : 1;
        general.Controls.AddRange(new Control[] { top, hide, startup, names, nameMode });
        general.Controls.Add(new Label { Text = "Modo:", Location = new Point(18, 151), AutoSize = true });
        var dockMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(190, 147), Width = 205 };
        dockMode.Items.AddRange(new object[] { "Acoplada", "Flotante" });
        dockMode.SelectedItem = config.DockMode;
        if (dockMode.SelectedIndex < 0) dockMode.SelectedIndex = 0;
        general.Controls.Add(dockMode);
        general.Controls.Add(new Label { Text = "Ubicación:", Location = new Point(18, 190), AutoSize = true });
        var location = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(190, 186), Width = 180 };
        location.Items.AddRange(new object[] { "Derecha", "Izquierda", "Arriba", "Abajo" });
        location.SelectedItem = config.Edge;
        if (location.SelectedIndex < 0) location.SelectedIndex = 0;
        general.Controls.Add(location);
        var powerTitle = new Label { Text = "Botones de energía en la barra:", AutoSize = true, Location = new Point(18, 292) };
        var lockButton = new CheckBox { Text = "Bloquear", AutoSize = true, Checked = config.ShowLock };
        var shutdown = new CheckBox { Text = "Apagar", AutoSize = true, Location = new Point(240, 292), Checked = config.ShowShutdown };
        var restart = new CheckBox { Text = "Reiniciar", AutoSize = true, Location = new Point(335, 292), Checked = config.ShowRestart };
        general.Controls.AddRange(new Control[] { powerTitle, lockButton, shutdown, restart });
        // Responsive layout: labels and inputs occupy separate rows so Windows 10
        // at 200% DPI cannot make them overlap. The page scrolls if necessary.
        general.Controls.Clear();
        var generalRows = PageRows(general);
        var languageChoice = new ComboBox { Name = "LanguageChoice", DropDownStyle = ComboBoxStyle.DropDownList, Width = 270 };
        languageChoice.Items.AddRange(new object[] { "Español (Chile)", "English", "Português (Brasil)" });
        languageChoice.SelectedIndex = DockLanguage.Normalize(config.Language) switch { "en" => 1, "pt" => 2, _ => 0 };
        // Place the language selector in the upper right, outside the cards.
        var languageHeader = general.Controls.OfType<Panel>()
            .FirstOrDefault(p => p.Dock == DockStyle.Top);
        if (languageHeader != null)
        {
            languageChoice.Width = 220;
            languageChoice.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            languageChoice.Margin = Padding.Empty;
            languageHeader.Controls.Add(languageChoice);
            void AlignLanguage()
            {
                languageChoice.Location = new Point(
                    Math.Max(320, languageHeader.ClientSize.Width - languageChoice.Width - 18), 16);
            }
            languageHeader.Resize += (_, _) => AlignLanguage();
            AlignLanguage();
        }
        generalRows.Controls.Add(SectionTitle("Comportamiento", "Visibilidad e inicio automático."));
        generalRows.Controls.Add(top);
        generalRows.Controls.Add(hide);
        generalRows.Controls.Add(startup);
        generalRows.Controls.Add(SectionTitle("Disposición", "Etiquetas y posición de la barra."));
        generalRows.Controls.Add(FieldRow(names, nameMode));
        generalRows.Controls.Add(FieldRow(new Label { Text = "Modo:", AutoSize = true }, dockMode));
        generalRows.Controls.Add(FieldRow(new Label { Text = "Ubicación:", AutoSize = true }, location));
        generalRows.Controls.Add(SectionTitle("Botones del sistema", "Accesos rápidos a funciones de Windows."));
        lockButton.Text = "Mostrar botón Bloquear";
        shutdown.Text = "Mostrar botón Apagar";
        restart.Text = "Mostrar botón Reiniciar";
        generalRows.Controls.Add(shutdown);
        generalRows.Controls.Add(restart);
        generalRows.Controls.Add(lockButton);
        generalRows.Controls.Add(new Label { Text = "Al ocultarse queda una franja para recuperar la barra con el mouse.", AutoSize = true });
        generalRows.SizeChanged += (_, _) => FitRows(generalRows);

        var networkWidget = new CheckBox { Text = "Mostrar tráfico de red (conexión principal, cada segundo)", AutoSize = true, Checked = config.ShowNetworkTraffic };
        var monitorsSection = SectionTitle("Información del sistema", "Monitores que aparecerán en el Dock.");
        generalRows.Controls.Add(monitorsSection);
        generalRows.Controls.Add(networkWidget);
        var cpuWidget = new CheckBox { Text = "Mostrar uso de CPU", AutoSize = true, Checked = config.ShowSystemUsage && config.ShowCpu };
        var ramWidget = new CheckBox { Text = "Mostrar uso de memoria RAM", AutoSize = true, Checked = config.ShowSystemUsage && config.ShowRam };
        generalRows.Controls.Add(cpuWidget);
        generalRows.Controls.Add(ramWidget);
        var networkAtEnd = new CheckBox { Text = "Anclar tráfico junto a los botones de energía (derecha / abajo)", AutoSize = true, Checked = config.NetworkAtEnd };
        generalRows.Controls.Add(networkAtEnd);
        var widget = new CheckBox { Text = "Mostrar indicador de uso de discos en la barra", Checked = config.ShowDiskWidget, Location = new Point(18, 20), AutoSize = true };
        var diskColors = new Dictionary<string,int>(config.DiskColors, StringComparer.OrdinalIgnoreCase);
        var hiddenDisks = new HashSet<string>(config.HiddenDiskLetters ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var widgetLabel = new Label { Text = "Cada unidad tiene su propio color. Selecciónalo con el botón de su fila.", Location = new Point(18, 54), AutoSize = true };
        var critical = new NumericUpDown { Location = new Point(150, 84), Width = 80, Minimum = 70, Maximum = 99, Value = Math.Clamp(config.CriticalPercent, 70, 99) };
        var diskPanel = new FlowLayoutPanel
        {
            Width = 755, Height = 285, AutoScroll = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Padding = new Padding(5)
        };
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.TotalSize <= 0) continue;
                string letter = drive.Name.Substring(0, 2).ToUpperInvariant();
                Color initialColor = Color.FromArgb(diskColors.TryGetValue(letter, out int existing) ? existing : config.DiskColorArgb);
                // Three rows per drive: visibility, preview, palette. No absolute
                // positions ever share the same vertical band.
                var line = new Panel { Width = 850, Height = 66, Margin = new Padding(3, 3, 3, 12) };
                var show = new CheckBox
                {
                    Text = $"Mostrar unidad {letter}",
                    Checked = !hiddenDisks.Contains(letter),
                    AutoSize = true, Location = new Point(8, 14)
                };
                show.CheckedChanged += (_, _) =>
                {
                    if (show.Checked) hiddenDisks.Remove(letter);
                    else hiddenDisks.Add(letter);
                    ScheduleLiveChange();
                };
                line.Controls.Add(show);
                // Compact color picker replaces the numerical disk-capacity preview.
                Color[] presets = {
                    Color.FromArgb(48, 130, 222),
                    Color.FromArgb(0, 202, 113),
                    Color.FromArgb(240, 105, 105)
                };
                var paletteButtons = new List<Button>();
                void SelectColor(Color selected)
                {
                    diskColors[letter] = selected.ToArgb();
                    ScheduleLiveChange();
                    foreach (var chip in paletteButtons)
                    {
                        if (chip.Tag is Color sample)
                        {
                            bool active = sample.ToArgb() == selected.ToArgb();
                            chip.FlatAppearance.BorderColor = active ? Color.FromArgb(18, 165, 185) : Color.DimGray;
                            chip.FlatAppearance.BorderSize = active ? 3 : 1;
                        }
                    }
                }
                for (int index = 0; index < presets.Length; index++)
                {
                    Color preset = presets[index];
                    var chip = new Button
                    {
                        Name = "DiskPaletteSwatch", Tag = preset,
                        FlatStyle = FlatStyle.Flat, BackColor = preset,
                        UseVisualStyleBackColor = false,
                        Size = new Size(52, 42),
                        FlatAppearance = { BorderSize = 2 },
                        Location = new Point(385 + index * 65, 10),
                        AccessibleName = index switch { 0 => "Azul", 1 => "Verde", _ => "Coral" }
                    };
                    chip.Click += (_, _) => SelectColor(preset);
                    paletteButtons.Add(chip);
                    line.Controls.Add(chip);
                }
                var custom = new Button
                {
                    Name = "DiskPaletteCustom", Text = "Otro color...",
                    Font = new Font("Segoe UI", 9f),
                    FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false,
                    Size = new Size(152, 44), Location = new Point(590, 9),
                    AccessibleName = "Elegir color personalizado"
                };
                custom.Click += (_, _) =>
                {
                    using var picker = new ColorDialog
                    {
                        Color = Color.FromArgb(diskColors.TryGetValue(letter, out int saved) ? saved : config.DiskColorArgb),
                        FullOpen = true
                    };
                    if (picker.ShowDialog(this) == DialogResult.OK) { SelectColor(picker.Color); ScheduleLiveChange(); }
                };
                line.Controls.Add(custom);
                SelectColor(initialColor);
                // Theme walkers may run on Shown; preserve swatch backgrounds afterward.
                Shown += (_, _) => { foreach (var chip in paletteButtons) chip.BackColor = (Color)chip.Tag!; };
                diskPanel.Controls.Add(line);
            }
            catch { }
        }

        // Disk settings use the same scrollable stack for 200% DPI.
        // Disk options now belong to General: keep disk colors and threshold intact.
        generalRows.Controls.Add(SectionTitle("Indicadores de discos", "Selecciona qué unidades mostrar y sus colores."));
        generalRows.Controls.Add(widget);
        generalRows.Controls.Add(widgetLabel);
        generalRows.Controls.Add(FieldRow(new Label { Text = "Avisar desde (%):", AutoSize = true }, critical));
        diskPanel.Height = 300;
        diskPanel.AutoScroll = true;
        generalRows.Controls.Add(diskPanel);

        var appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        string versionText = appVersion == null ? "versión desconocida" : $"v{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}";
        var info = new Label { Text = $"JANUS Dock — {versionText}\n\nBarra de accesos para Windows 10/11.\nUnidades de disco con iconos Windows/Lucide y apertura en Explorer.\nCPU, RAM y red con indicadores compactos y promedio de 60 s.\nEfecto lupa y Ctrl + rueda para cambiar tamaño de iconos.\nOpciones multidioma y arranque con Windows.\n\nC# · Windows Forms · .NET 8\nMomocrackcorp · Ñuble, Chile\nAutoría: Omar Aguila", Location = new Point(104, 20), Size = new Size(520, 280) };
        var appIcon = new PictureBox { Location = new Point(20, 25), Size = new Size(64, 64), SizeMode = PictureBoxSizeMode.Zoom };
        try { using var ico = Icon.ExtractAssociatedIcon(Application.ExecutablePath); appIcon.Image = ico?.ToBitmap() ?? SystemIcons.Application.ToBitmap(); }
        catch { appIcon.Image = SystemIcons.Application.ToBitmap(); }
        about.Controls.Add(appIcon);
        about.Controls.Add(info);
        var updates = new Button { Text = "Buscar actualizaciones en GitHub", Location = new Point(20, 235), Size = new Size(260, 35) };
        updates.Click += (_, _) => Process.Start(new ProcessStartInfo("https://github.com/momocrackcorp/janus-dock/releases/latest") { UseShellExecute = true });
        about.Controls.Add(updates);
        about.Controls.Clear();
        var aboutRows = PageRows(about);
        appIcon.Size = new Size(64, 64);
        info.AutoSize = true;
        info.MaximumSize = new Size(480, 0);
        // The project photo is embedded and loaded automatically.
        // No file chooser or external image is needed for the About page.
        var aboutPicture = new PictureBox {
            Size = new Size(280, 150),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(227, 238, 246),
            Margin = new Padding(5, 6, 5, 7)
        };
        try { aboutPicture.Image = AboutDefaultPhoto.Load(); }
        catch { aboutPicture.Image = null; }
        aboutRows.Controls.Add(SectionTitle("JANUS Dock", "Información del proyecto."));
        aboutRows.Controls.Add(aboutPicture);
        aboutRows.Controls.Add(appIcon);
        aboutRows.Controls.Add(info);
        updates.AutoSize = true;
        aboutRows.Controls.Add(updates);
        aboutRows.SizeChanged += (_, _) => FitRows(aboutRows);


        // Separate the functional panels from the appearance configuration.
        var accessRows = PageRows(accessPage);
        var systemRows = PageRows(systemPage);
        void MoveFromSection(FlowLayoutPanel source, FlowLayoutPanel target, Control section)
        {
            int start = source.Controls.GetChildIndex(section);
            // Controls order in FlowLayoutPanel is the logical top-to-bottom order.
            var items = source.Controls.Cast<Control>().Skip(start).ToArray();
            foreach (var control in items) target.Controls.Add(control);
        }
        MoveFromSection(appearanceRows, accessRows, accessesSection);
        MoveFromSection(generalRows, systemRows, monitorsSection);

        // Compact overview in General. Detailed CPU/RAM and disk options remain
        // in Sistema; these switches operate the same underlying settings.
        var quickNetwork = new CheckBox { Text = "Mostrar tráfico de red",
            Checked = networkWidget.Checked, AutoSize = true };
        var quickUsage = new CheckBox { Text = "Mostrar uso de CPU y RAM",
            Checked = cpuWidget.Checked && ramWidget.Checked, AutoSize = true };
        var quickNetworkEnd = new CheckBox { Text = "Tráfico junto a botones de energía",
            Checked = networkAtEnd.Checked, AutoSize = true };
        quickNetwork.CheckedChanged += (_, _) => networkWidget.Checked = quickNetwork.Checked;
        networkWidget.CheckedChanged += (_, _) => quickNetwork.Checked = networkWidget.Checked;
        quickUsage.CheckedChanged += (_, _) => { cpuWidget.Checked = quickUsage.Checked; ramWidget.Checked = quickUsage.Checked; };
        void SyncUsageQuick() => quickUsage.Checked = cpuWidget.Checked && ramWidget.Checked;
        cpuWidget.CheckedChanged += (_, _) => SyncUsageQuick();
        ramWidget.CheckedChanged += (_, _) => SyncUsageQuick();
        quickNetworkEnd.CheckedChanged += (_, _) => networkAtEnd.Checked = quickNetworkEnd.Checked;
        networkAtEnd.CheckedChanged += (_, _) => quickNetworkEnd.Checked = networkAtEnd.Checked;
        generalRows.Controls.Add(SectionTitle("Información del sistema", "Indicadores compactos en la barra."));
        generalRows.Controls.Add(quickNetwork);
        generalRows.Controls.Add(quickUsage);
        generalRows.Controls.Add(quickNetworkEnd);
        accessRows.SizeChanged += (_, _) => FitRows(accessRows);
        systemRows.SizeChanged += (_, _) => FitRows(systemRows);

        PackCards(generalRows);
        PackCards(appearanceRows);
        PackCards(accessRows);
        PackCards(systemRows);
        PackCards(aboutRows);
        // Four real cards arranged in two columns on desktop-width windows,
        // with a responsive single-column fallback on narrow screens.
        var generalDashboard = new Panel {
            Name = "GeneralDashboard", BackColor = Color.FromArgb(229, 236, 244),
            Margin = Padding.Empty
        };
        var generalCards = generalRows.Controls.OfType<SettingsCardPanel>().ToArray();
        if (generalCards.Length != 4)
            throw new InvalidOperationException("La página General requiere cuatro tarjetas.");
        foreach (var card in generalCards) generalDashboard.Controls.Add(card);
        generalRows.Controls.Add(generalDashboard);
        // Refit cards after modern controls are installed at runtime.
        Shown += (_, _) => {
            InstallModernSelectors(this);
            InstallModernToggles(this);
            FitRows(generalRows);
            FitRows(appearanceRows);
            FitRows(accessRows);
            FitRows(systemRows);
            FitRows(aboutRows);
        };

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(8), AutoScroll = false };
        Controls.Add(bar); bar.BringToFront();
        var cancel = new Button { Text = "Cancelar", AutoSize = true, MinimumSize = new Size(110, 36), DialogResult = DialogResult.Cancel };
        var apply = new Button { Text = "Guardar y cerrar", AutoSize = true, MinimumSize = new Size(150, 36), DialogResult = DialogResult.OK };
        var previewButton = new Button { Text = "Aplicar", AutoSize = true, MinimumSize = new Size(110, 36) };
        bar.Controls.AddRange(new Control[] { apply, previewButton, cancel });
        AcceptButton = apply; CancelButton = cancel;
        Shown += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;
            Size = new Size(Math.Min(1240, area.Width - 16), Math.Min(795, area.Height - 16));
            Location = new Point(area.Left + Math.Max(0, (area.Width - Width) / 2), area.Top + Math.Max(0, (area.Height - Height) / 2));
            bar.Height = Math.Max(64, apply.PreferredSize.Height + 20);
            FitRows(generalRows);
            FitRows(appearanceRows);
            FitRows(accessRows);
            FitRows(systemRows);
            FitRows(aboutRows);
            ShowPage(initialTab == "Apariencia" ? 1 : initialTab == "Acerca de" ? 4 : 0);
        };
        void CaptureSettings()
        {
            Language = languageChoice.SelectedIndex switch { 1 => "en", 2 => "pt", _ => "es" };
            AlwaysOnTop = top.Checked;
            AutoHide = hide.Checked;
            StartWithWindows = startup.Checked;
            NameMode = nameMode.SelectedIndex == 0 ? "None" : "Tooltip";
            ShowNames = NameMode == "Written";
            DockMode = dockMode.SelectedItem?.ToString() ?? "Acoplada";
            ShowDiskWidget = widget.Checked;
            ShowNetworkTraffic = networkWidget.Checked;
            ShowCpu = cpuWidget.Checked;
            ShowRam = ramWidget.Checked;
            ShowSystemUsage = ShowCpu || ShowRam;
            NetworkAtEnd = networkAtEnd.Checked;
            IconSize = (int)size.Value;
            IconSpacing = spacingSlider.Value;
            BarWidth = (int)barWidth.Value;
            BarHeight = (int)barHeight.Value;
            IconStyle = iconStyle.SelectedIndex == 1 ? "Lucide" : "Windows";
            BarColorMode = barColorMode.SelectedIndex == 1 ? "Custom" : barColorMode.SelectedIndex == 2 ? "Classic" : "Windows";
            CustomBarColorArgb = selectedBarArgb;
            OrderedItems = new List<DockItem>(ordered);
            Edge = (string)location.SelectedItem!;
            DiskColorArgb = config.DiskColorArgb;
            DiskColors = diskColors;
            HiddenDiskLetters = hiddenDisks.OrderBy(x => x).ToList();
            HoverEffect = hoverEffect.SelectedIndex == 0 ? "Ninguno" : hoverEffect.SelectedIndex == 1 ? "Foco" : "Lupa";
            HoverAnimation = HoverEffect == "Lupa";
            ZoomPercent = zoomValues[Math.Max(0, zoomChoice.SelectedIndex)];
            ShowLock = lockButton.Checked;
            ShowShutdown = shutdown.Checked;
            ShowRestart = restart.Checked;
            CriticalPercent = (int)critical.Value;
        }
        // Debounced live Apply: user never has to manually refresh the dock.
        // Cancel restores the original snapshot in Program.ShowOptions.
        var liveTimer = new System.Windows.Forms.Timer { Interval = 300 };
        liveTimer.Tick += (_, _) =>
        {
            liveTimer.Stop();
            if (IsDisposed || Disposing) return;
            CaptureSettings();
            ApplyRequested?.Invoke(this);
        };
        void SchedulePreview()
        {
            if (!IsHandleCreated || IsDisposed) return;
            liveTimer.Stop();
            liveTimer.Start();
        }
        void WatchControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is CheckBox check) check.CheckedChanged += (_, _) => SchedulePreview();
                if (control is ComboBox choice) choice.SelectedIndexChanged += (_, _) => SchedulePreview();
                if (control is NumericUpDown number) number.ValueChanged += (_, _) => SchedulePreview();
                if (control is TrackBar track) track.ValueChanged += (_, _) => SchedulePreview();
                WatchControls(control);
            }
        }
        liveChange = SchedulePreview;
        WatchControls(this);
        FormClosed += (_, _) => liveTimer.Dispose();
        DockLanguage.TranslateControls(this, config.Language);
        languageChoice.SelectedIndexChanged += (_, _) =>
        {
            string selected = languageChoice.SelectedIndex switch { 1 => "en", 2 => "pt", _ => "es" };
            DockLanguage.TranslateControls(this, selected);
            Text = DockLanguage.Translate("Opciones de JANUS Dock", selected);
        };
        previewButton.Click += (_, _) => { CaptureSettings(); ApplyRequested?.Invoke(this); };
        apply.Click += (_, _) => CaptureSettings();
    }
    private static void InstallModernToggles(Control parent)
    {
        foreach (Control child in parent.Controls.Cast<Control>().ToArray())
        {
            if (child is CheckBox check)
            {
                int index = parent.Controls.GetChildIndex(check);
                var position = check.Location;
                var anchor = check.Anchor;
                var dock = check.Dock;
                parent.Controls.Remove(check);
                var toggle = new ModernToggle(check)
                {
                    Location = position, Anchor = anchor, Dock = dock,
                    Margin = check.Margin, Font = check.Font,
                    ForeColor = parent.ForeColor
                };
                parent.Controls.Add(toggle);
                parent.Controls.SetChildIndex(toggle, index);
            }
            else InstallModernToggles(child);
        }
    }
    private static void InstallModernSelectors(Control parent)
    {
        foreach (Control child in parent.Controls.Cast<Control>().ToArray())
        {
            if (child is ComboBox combo)
            {
                int index = parent.Controls.GetChildIndex(combo);
                Point position = combo.Location;
                AnchorStyles anchor = combo.Anchor;
                DockStyle dock = combo.Dock;
                parent.Controls.Remove(combo);
                var styled = new ModernSelector(combo)
                {
                    Location = position,
                    Anchor = anchor,
                    Dock = dock,
                    Font = combo.Font,
                    Margin = combo.Margin
                };
                parent.Controls.Add(styled);
                parent.Controls.SetChildIndex(styled, index);
            }
            else InstallModernSelectors(child);
        }
    }
    private static void StyleWindowsControls(Control parent)
    {
        bool light = SystemColors.Window.GetBrightness() >= 0.5f;
        Color background = SystemColors.Control;
        Color foreground = SystemColors.ControlText;
        if (parent.Name != "DiskPaletteSwatch") parent.BackColor = background;
        if (parent.Name != "DiskPaletteSwatch") parent.ForeColor = foreground;
        foreach (Control child in parent.Controls)
        {
            if (child is Button b && b.Name != "DiskPaletteSwatch" && b.Name != "DiskPaletteCustom") { b.FlatStyle = FlatStyle.System; b.UseVisualStyleBackColor = true; if (b.Text == "Color...") { b.UseVisualStyleBackColor = false; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 1; b.AutoSize = false; b.Height = 36; } }
            if (child is ComboBox cb)
            {
                cb.FlatStyle = FlatStyle.Flat;
                cb.BackColor = background;
                cb.ForeColor = foreground;
                cb.IntegralHeight = false;
                cb.ItemHeight = Math.Max(cb.ItemHeight, 24);
                if (cb.DropDownStyle == ComboBoxStyle.DropDownList && cb.DrawMode != DrawMode.OwnerDrawFixed)
                {
                    cb.DrawMode = DrawMode.OwnerDrawFixed;
                    cb.DrawItem += (_, e) =>
                    {
                        if (e.Index < 0) return;
                        bool selected = (e.State & DrawItemState.Selected) != 0;
                        using var fill = new SolidBrush(selected ? Color.FromArgb(23, 158, 185) : cb.BackColor);
                        e.Graphics.FillRectangle(fill, e.Bounds);
                        TextRenderer.DrawText(e.Graphics, cb.Items[e.Index]?.ToString() ?? "", cb.Font,
                            e.Bounds, selected ? Color.White : cb.ForeColor,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    };
                }
            }
            if (child is CheckBox ch) ch.FlatStyle = FlatStyle.System;
            if (child is TabPage page) page.BackColor = background;
            StyleWindowsControls(child);
        }
    }
    private static FlowLayoutPanel PageRows(ScrollableControl page)
    {
        page.AutoScroll = true;
        var header = new Panel {
            Dock = DockStyle.Top, Height = 82,
            BackColor = Color.FromArgb(229, 236, 244)
        };
        string description = page.Name switch {
            "General" => "Comportamiento y funciones principales del Dock.",
            "Apariencia" => "Personaliza el aspecto visual del Dock.",
            "Accesos" => "Organiza las aplicaciones y los separadores.",
            "Sistema" => "Monitores de red, CPU, memoria y discos.",
            _ => "Proyecto, versión, imagen y créditos."
        };
        header.Controls.Add(new Label {
            Text = page.Name, Location = new Point(22, 15), AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 41, 65)
        });
        header.Controls.Add(new Label {
            Text = description, Location = new Point(24, 55), AutoSize = true,
            Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(91, 110, 132)
        });
        page.Controls.Add(header);
        var rows = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = false, Padding = new Padding(13, 6, 10, 8), Margin = Padding.Empty,
            BackColor = Color.FromArgb(229, 236, 244),
            Location = new Point(0, 82), Width = Math.Max(340, page.ClientSize.Width - 16),
            Height = 630, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        page.Controls.Add(rows);
        page.Tag = rows;
        header.BringToFront();
        page.Resize += (_, _) => FitRows(rows);
        return rows;
    }

    private static Control SectionTitle(string title, string description)
    {
        var group = new FlowLayoutPanel {
            Name = "SettingsSectionTitle",
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 3, 4, 1), Margin = new Padding(0, 0, 0, 2)
        };
        group.Controls.Add(new Label { Text = title, AutoSize = true,
            ForeColor = Color.FromArgb(27, 121, 169),
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold) });
        group.Controls.Add(new Label { Text = description, AutoSize = true,
            ForeColor = Color.FromArgb(103, 115, 129),
            Font = new Font("Segoe UI", 9f) });
        return group;
    }

    private static FlowLayoutPanel FieldRow(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            Name = "SettingsFieldRow",
            AutoSize = false, Height = 42,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            Margin = new Padding(0, 0, 0, 2), Padding = Padding.Empty,
            MinimumSize = new Size(250, 42)
        };
        foreach (var control in controls)
        {
            control.Margin = new Padding(3, 3, 10, 2);
            if (control == controls[0] && control is Label mainLabel)
            {
                mainLabel.AutoSize = false;
                mainLabel.Width = controls.Length >= 4 ? 125 : controls.Length >= 3 ? 205 : 270;
                mainLabel.Height = 32;
                mainLabel.TextAlign = ContentAlignment.MiddleLeft;
            }
            if (control is ComboBox combo) combo.Width = Math.Min(combo.Width, 235);
            row.Controls.Add(control);
        }
        return row;
    }

    private static void PackCards(FlowLayoutPanel rows)
    {
        var source = rows.Controls.Cast<Control>().ToArray();
        rows.Controls.Clear();
        SettingsCardPanel? card = null;
        foreach (Control control in source)
        {
            if (card == null || control.Name == "SettingsSectionTitle")
            {
                card = new SettingsCardPanel();
                rows.Controls.Add(card);
            }
            card.Controls.Add(control);
            if (control is FlowLayoutPanel layout && layout.Name is "SettingsFieldRow" or "SettingsSectionTitle")
            {
                layout.BackColor = card.BackColor;
                foreach (Control inner in layout.Controls)
                    if (inner is Label label) label.BackColor = card.BackColor;
            }
            if (control is Label textLabel) textLabel.BackColor = card.BackColor;
        }
    }

    private static void FitCard(SettingsCardPanel card, int parentWidth)
    {
        card.Width = Math.Max(300, parentWidth - 22);
        int usable = Math.Max(260, card.Width - card.Padding.Horizontal - 10);
        foreach (Control child in card.Controls)
        {
            if (child is FlowLayoutPanel layout)
            {
                if (layout.Name == "SettingsFieldRow")
                {
                    layout.Width = usable;
                    // Prevent clipped selectors when two dashboard columns are shown.
                    if (layout.Controls.Count == 2 && layout.Controls[0] is Label caption)
                    {
                        var selector = layout.Controls[1];
                        if (selector is ComboBox || selector is ModernSelector)
                        {
                            caption.Width = Math.Clamp(usable / 3, 95, 180);
                            selector.Width = Math.Clamp(usable - caption.Width - 27, 130, 235);
                        }
                    }
                    layout.Height = layout.Controls.OfType<TrackBar>().Any() ? 58 : Math.Max(42, layout.Font.Height * 2 + 10);
                }
                else if (layout.Name == "SettingsSectionTitle")
                {
                    layout.MaximumSize = new Size(usable, 0);
                    layout.PerformLayout();
                }
                else if (layout.Controls.Count > 0 && layout.Controls[0] is Button)
                {
                    layout.Width = usable;
                    layout.Height = 96;
                }
                else
                {
                    layout.Width = usable;
                }
            }
            if (child is ModernToggle toggle)
            {
                toggle.BackColor = card.BackColor;
                toggle.RightAligned = true;
                toggle.Width = usable;
                toggle.Height = Math.Max(34, toggle.Font.Height + 16);
                toggle.Invalidate();
            }
            if (child is ListBox list) list.Width = usable;
            if (child is Label label) label.MaximumSize = new Size(usable, 0);
            if (child is FlowLayoutPanel diskFlow &&
                diskFlow.Controls.OfType<Panel>().Any())
            {
                diskFlow.Width = usable;
                diskFlow.Height = 290;
                foreach (Panel line in diskFlow.Controls.OfType<Panel>())
                    line.Width = Math.Max(800, usable - 10);
            }
        }
        card.PerformLayout();
        card.Height = card.Padding.Vertical + card.Controls.Cast<Control>()
            .Sum(x => x.Height + x.Margin.Vertical) + 2;
    }

    private static void FitGeneralDashboard(Panel dashboard, int availableWidth)
    {
        dashboard.Width = Math.Max(300, availableWidth - 14);
        var cards = dashboard.Controls.OfType<SettingsCardPanel>().ToArray();
        if (cards.Length != 4) return;
        const int gap = 14;
        bool doubleColumn = dashboard.Width >= 640;
        int cellWidth = doubleColumn ? (dashboard.Width - gap) / 2 : dashboard.Width;
        foreach (var card in cards)
        {
            card.Margin = Padding.Empty;
            FitCard(card, cellWidth + 22);
        }
        if (doubleColumn)
        {
            // Row 1: behaviour + arrangement. Row 2: system actions + monitors.
            int topHeight = Math.Max(cards[0].Height, cards[1].Height);
            cards[0].Location = new Point(0, 0);
            cards[1].Location = new Point(cellWidth + gap, 0);
            cards[2].Location = new Point(0, topHeight + gap);
            cards[3].Location = new Point(cellWidth + gap, topHeight + gap);
            dashboard.Height = topHeight + gap + Math.Max(cards[2].Height, cards[3].Height) + 3;
        }
        else
        {
            int y = 0;
            foreach (var card in cards)
            {
                card.Location = new Point(0, y);
                y += card.Height + gap;
            }
            dashboard.Height = y;
        }
    }

    private static void FitRows(FlowLayoutPanel rows)
    {
        var page = rows.Parent;
        if (page == null) return;
        int width = Math.Max(290, Math.Min(1080, page.ClientSize.Width - 28));
        rows.Width = width;
        foreach (Control control in rows.Controls)
        {
            if (control is ListBox list) list.Width = Math.Max(240, width - 24);
            if (control is FlowLayoutPanel diskFlow && diskFlow.Controls.OfType<Panel>().Any())
            {
                diskFlow.Width = Math.Max(250, width - 24);
                foreach (Panel line in diskFlow.Controls.OfType<Panel>())
                {
                    line.Width = Math.Max(850, diskFlow.ClientSize.Width - 24);
                    // Disk palette controls manage their own independent positions.
                    // Never relocate the first swatch to the far right or stretch
                    // the preview underneath the palette.
                    var preview = line.Controls.OfType<DiskUsageControl>().FirstOrDefault();
                    if (preview != null) preview.Width = Math.Max(330, line.Width - 28);
                }
            }
            if (control is FlowLayoutPanel row)
            {
                row.Width = Math.Max(250, width - 24);
                if (row.Controls.Count > 0 && row.Controls[0] is Button)
                {
                    // Button toolbar: adjust its height to the number of wrapped lines.
                    int usable = Math.Max(160, row.ClientSize.Width - row.Padding.Horizontal - 8);
                    int lineWidth = 0, lineHeight = 0, height = row.Padding.Vertical + 8;
                    foreach (Control button in row.Controls)
                    {
                        int itemWidth = button.Width + button.Margin.Horizontal;
                        int itemHeight = button.Height + button.Margin.Vertical;
                        if (lineWidth > 0 && lineWidth + itemWidth > usable)
                        {
                            height += lineHeight;
                            lineWidth = 0;
                            lineHeight = 0;
                        }
                        lineWidth += itemWidth;
                        lineHeight = Math.Max(lineHeight, itemHeight);
                    }
                    row.Height = height + lineHeight;
                }
            }
            if (control is Label label) label.MaximumSize = new Size(Math.Max(230, width - 25), 0);
            if (control is CheckBox check) check.MaximumSize = new Size(Math.Max(230, width - 25), 0);
            if (control is FlowLayoutPanel flow && flow != rows)
                foreach (Control child in flow.Controls)
                    if (child is Label rowLabel) rowLabel.MaximumSize = new Size(Math.Max(225, width - 38), 0);
        }
        foreach (SettingsCardPanel card in rows.Controls.OfType<SettingsCardPanel>()) FitCard(card, width);
        foreach (Panel dashboard in rows.Controls.OfType<Panel>().Where(p => p.Name == "GeneralDashboard"))
            FitGeneralDashboard(dashboard, width - 5);
        rows.PerformLayout();
        rows.Height = rows.Controls.Cast<Control>().Sum(x => x.Height + x.Margin.Vertical) + rows.Padding.Vertical + 10;
    }

    public static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunPath);
        return key?.GetValue(RunValue) != null;
    }
    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunPath, true);
        if (enabled) key?.SetValue(RunValue, $"\"{Application.ExecutablePath}\"");
        else key?.DeleteValue(RunValue, false);
    }
}

public sealed class DiskUsageControl : Control
{
    readonly string name;
    readonly string rootPath;
    readonly double ratio;
    string mediaType = "";
    public bool UseLucide { get; set; }
    public int IconPixels { get; set; } = 40;
    public bool ZoomEnabled { get; set; } = true;
    public float ZoomFactor { get; set; } = 1.8f;
    bool hovered;
    float hoverProgress;
    readonly System.Windows.Forms.Timer hoverTimer = new() { Interval = 16 };
    static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Task<string>> MediaCache = new();
    readonly string details;
    readonly bool compact;
    Color accent;
    int threshold;
    public DiskUsageControl(DriveInfo drive, Color color, int criticalPercent, bool compactMode = false)
    {
        name = drive.Name.TrimEnd('\\');
        rootPath = drive.RootDirectory.FullName;
        if (compactMode)
        {
            Cursor = Cursors.Hand;
            hoverTimer.Tick += (_, _) =>
            {
                float target = hovered && ZoomEnabled ? 1f : 0f;
                hoverProgress += (target - hoverProgress) * .28f;
                if (Math.Abs(target - hoverProgress) < .01f) { hoverProgress = target; hoverTimer.Stop(); }
                Invalidate();
            };
            MouseEnter += (_, _) => { hovered = true; hoverTimer.Start(); };
            MouseLeave += (_, _) => { hovered = false; hoverTimer.Start(); };
            Click += (_, _) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", rootPath) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show("No se pudo abrir la unidad: " + ex.Message, "JANUS Dock", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            var lookup = MediaCache.GetOrAdd(rootPath, p => Task.Run(() => DetectMediaType(p)));
            _ = lookup.ContinueWith(t =>
            {
                if (IsDisposed || t.IsFaulted || t.IsCanceled || !IsHandleCreated) return;
                try { BeginInvoke(new Action(() => { mediaType = t.Result; Invalidate(); })); } catch { }
            });
        }
        ratio = Math.Clamp((double)(drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize, 0, 1);
        details = $"{(drive.TotalSize - drive.TotalFreeSpace) / 1073741824.0:F1} GB usados · {drive.TotalFreeSpace / 1073741824.0:F1} GB libres";
        accent = color;
        threshold = criticalPercent;
        compact = compactMode;
        DoubleBuffered = true;
    }
    static string DetectMediaType(string root)
    {
        try
        {
            char letter = char.ToUpperInvariant(root[0]);
            if (letter < 'A' || letter > 'Z') return "";
            // Windows storage cmdlets; unknown/virtual media is intentionally not labelled HDD.
            string command = "$p=Get-Partition -DriveLetter " + letter + " -ErrorAction Stop; " +
                "$d=$p | Get-Disk -ErrorAction Stop; " +
                "$physical=$d | Get-PhysicalDisk -ErrorAction SilentlyContinue; " +
                "if($physical){$physical | Select-Object -First 1 -ExpandProperty MediaType}";
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo("powershell.exe")
            {
                Arguments = "-NoProfile -NonInteractive -Command \"" + command + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            process.Start();
            if (!process.WaitForExit(4000)) { try { process.Kill(); } catch { } return ""; }
            string result = process.StandardOutput.ReadToEnd().Trim();
            return result.Equals("SSD", StringComparison.OrdinalIgnoreCase) ? "SSD" :
                result.Equals("HDD", StringComparison.OrdinalIgnoreCase) ? "HDD" : "";
        }
        catch { return ""; }
    }
    public void SetAppearance(Color color, int criticalPercent) { accent = color; threshold = criticalPercent; Invalidate(); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { hoverTimer.Stop(); hoverTimer.Dispose(); }
        base.Dispose(disposing);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var ink = new SolidBrush(ForeColor);
        if (!compact)
        {
            using var ringBack = new Pen(Color.FromArgb(190,197,207), 8);
            using var ringFill = new Pen(accent, 8);
            var ring = new Rectangle(9, 9, 46, 46);
            g.DrawEllipse(ringBack, ring);
            g.DrawArc(ringFill, ring, -90, (float)(ratio * 360));
            using var title = new Font("Segoe UI", 10, FontStyle.Bold);
            using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(name, title, ink, new RectangleF(12, 14, 42, 38), fmt);
            using var sub = new Font("Segoe UI", 9);
            g.DrawString(details, sub, ink, 80, 28);
            return;
        }

        // The disk itself is the usage indicator: Windows is a shaded device,
        // Lucide is an outline device. Both scale with IconPixels.
        // Animate from a smaller resting icon toward the largest drawable size.
        // The old code immediately hit the clip limit, making hover imperceptible.
        float room = Math.Max(16f, Math.Min(Width - 4, Height - 4));
        float normal = Math.Min(room * .78f, IconPixels * .92f);
        float magnified = Math.Min(room, normal * Math.Clamp(ZoomFactor, 1f, 3.4f));
        float baseSize = normal + (magnified - normal) * (ZoomEnabled ? hoverProgress : 0f);
        float sz = Math.Max(15f, baseSize);
        float x = (Width - sz) / 2f, y = (Height - sz) / 2f;
        g.TranslateTransform(x, y);
        g.ScaleTransform(sz / 64f, sz / 64f);
        using var outline = new Pen(UseLucide ? ForeColor : Color.FromArgb(170, 191, 208), UseLucide ? 3.8f : 2.2f);
        outline.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
        using var fill = new SolidBrush(UseLucide ? BackColor : Color.FromArgb(70, 91, 108));
        using var face = new SolidBrush(UseLucide ? BackColor : Color.FromArgb(48, 62, 75));
        using var progress = new SolidBrush(accent);
        PointF[] shell = { new(14,21), new(47,21), new(55,43), new(55,55), new(8,55), new(8,43) };
        g.FillPolygon(fill, shell);
        g.DrawPolygon(outline, shell);
        g.FillRectangle(face, 11, 43, 41, 9);
        g.DrawLine(outline, 10, 42, 53, 42);
        // Coloured progress rail fills left-to-right as occupied space grows.
        using var rail = new SolidBrush(Color.FromArgb(105, 118, 130));
        g.FillRectangle(rail, 13, 47, 33, 5);
        g.FillRectangle(progress, 13, 47, (float)(33 * ratio), 5);
        g.FillEllipse(Brushes.LightGreen, 49, 47, 3, 3);
        if (!UseLucide)
        {
            using var highlight = new Pen(Color.FromArgb(215, 226, 237), 1.5f);
            g.DrawLine(highlight, 16, 24, 44, 24);
        }
        // Centered and prominent, as in the approved mockup.
        // Keep the badge large enough relative to the drive at every scale.
        const float badgeX = 14f, badgeY = 4f, badgeDiameter = 36f;
        using (var circle = new SolidBrush(Color.FromArgb(204, 13, 31, 43)))
            g.FillEllipse(circle, badgeX, badgeY, badgeDiameter, badgeDiameter);
        using (var halo = new Pen(Color.FromArgb(190, 190, 221, 237), 1.6f))
            g.DrawEllipse(halo, badgeX, badgeY, badgeDiameter, badgeDiameter);
        using (var letterFont = new Font("Segoe UI", 21f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var letterBrush = new SolidBrush(Color.White))
        using (var letterLayout = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            g.DrawString(name, letterFont, letterBrush, new RectangleF(badgeX, badgeY, badgeDiameter, badgeDiameter), letterLayout);
        if (ratio * 100 >= threshold)
        {
            using var warning = new SolidBrush(Color.OrangeRed);
            g.FillEllipse(warning, 52, 11, 9, 9);
        }
        g.ResetTransform();
    }
}
