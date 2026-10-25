using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JanusDock;

// The Spanish UI text is the stable lookup key. Programmatic option values
// (Acoplada, Abajo, Tooltip...) remain unchanged to protect saved settings.
internal static class DockLanguage
{
    public static string Normalize(string? code) => code is "en" or "pt" ? code : "es";
    static readonly Dictionary<string, (string en, string pt)> Strings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Opciones de JANUS Dock"] = ("JANUS Dock Settings", "Configurações do JANUS Dock"),
        ["General"] = ("General", "Geral"),
        ["Apariencia"] = ("Appearance", "Aparência"),
        ["Acerca de"] = ("About", "Sobre"),
        ["Mantener siempre visible (encima de otras ventanas)"] = ("Always on top of other windows", "Manter sempre visível sobre outras janelas"),
        ["Ocultar automáticamente al alejar el puntero"] = ("Auto-hide when the pointer moves away", "Ocultar automaticamente ao afastar o ponteiro"),
        ["Cargar JANUS Dock al iniciar Windows"] = ("Start JANUS Dock with Windows", "Iniciar JANUS Dock com o Windows"),
        ["Nombres de accesos:"] = ("Shortcut labels:", "Nomes dos atalhos:"),
        ["Modo:"] = ("Mode:", "Modo:"),
        ["Ubicación:"] = ("Position:", "Posição:"),
        ["Botones de energía en la barra:"] = ("Power buttons on the dock:", "Botões de energia na barra:"),
        ["Apagar"] = ("Shut down", "Desligar"),
        ["Reiniciar"] = ("Restart", "Reiniciar"),
        ["Monitor de red"] = ("Network monitor", "Monitor de rede"),
        ["Mostrar tráfico de red (conexión principal, cada segundo)"] = ("Show network traffic (primary connection, every second)", "Mostrar tráfego de rede (conexão principal, a cada segundo)"),
        ["Mostrar uso de CPU y memoria RAM (cada segundo)"] = ("Show CPU and RAM usage (every second)", "Mostrar uso de CPU e RAM (a cada segundo)"),
        ["Anclar tráfico junto a los botones de energía (derecha / abajo)"] = ("Place network traffic next to power buttons", "Posicionar tráfego de rede junto aos botões de energia"),
        ["Indicadores de discos"] = ("Disk indicators", "Indicadores de discos"),
        ["Mostrar indicador de uso de discos en la barra"] = ("Show disk usage indicators on the dock", "Mostrar indicadores de disco na barra"),
        ["Avisar desde (%):"] = ("Warn at (%):", "Alertar a partir de (%):"),
        ["Tamaño de iconos:"] = ("Icon size:", "Tamanho dos ícones:"),
        ["Dimensiones de la barra"] = ("Dock dimensions", "Dimensões da barra"),
        ["Ancho barra vertical (px):"] = ("Vertical dock width (px):", "Largura da barra vertical (px):"),
        ["Alto barra horizontal (px):"] = ("Horizontal dock height (px):", "Altura da barra horizontal (px):"),
        ["Activar efecto lupa al pasar el mouse"] = ("Enable icon magnification on hover", "Ativar ampliação de ícones ao passar o mouse"),
        ["Ampliación de la lupa:"] = ("Magnification:", "Ampliação:"),
        ["Estilo de los iconos:"] = ("Icon style:", "Estilo dos ícones:"),
        ["Cancelar"] = ("Cancel", "Cancelar"),
        ["Aplicar"] = ("Apply", "Aplicar"),
        ["Guardar y cerrar"] = ("Save and close", "Salvar e fechar"),
        ["Buscar actualizaciones en GitHub"] = ("Check for updates on GitHub", "Verificar atualizações no GitHub"),
        ["Otro color..."] = ("Other color...", "Outra cor..."),
        ["Agregar"] = ("Add", "Adicionar"),
        ["Organizar barra"] = ("Organize dock", "Organizar barra"),
        ["Opciones..."] = ("Settings...", "Configurações..."),
        ["Refrescar barra"] = ("Refresh dock", "Atualizar barra"),
        ["Salir de JANUS Dock"] = ("Exit JANUS Dock", "Sair do JANUS Dock")
    };
    public static string Translate(string original, string? language)
    {
        language = Normalize(language);
        // Resolve text translated by a previous selection back to its Spanish key.
        foreach (var pair in Strings)
            if (string.Equals(original, pair.Value.en, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(original, pair.Value.pt, StringComparison.OrdinalIgnoreCase))
            { original = pair.Key; break; }
        if (original.StartsWith("Show drive ", StringComparison.OrdinalIgnoreCase))
            original = "Mostrar unidad " + original["Show drive ".Length..];
        if (original.StartsWith("Mostrar unidade ", StringComparison.OrdinalIgnoreCase))
            original = "Mostrar unidad " + original["Mostrar unidade ".Length..];
        if (language == "es") return original;
        if (Strings.TryGetValue(original, out var entry))
            return language == "pt" ? entry.pt : entry.en;
        if (original.StartsWith("Mostrar unidad ", StringComparison.OrdinalIgnoreCase))
            return (language == "pt" ? "Mostrar unidade " : "Show drive ") + original["Mostrar unidad ".Length..];
        return original;
    }
    public static void TranslateControls(Control parent, string? language)
    {
        foreach (Control child in parent.Controls)
        {
            // Do not translate ComboBox values: existing event handlers use
            // their Spanish values as internal identifiers.
            if (child is not ComboBox && child is not TextBox && !string.IsNullOrWhiteSpace(child.Text))
                child.Text = Translate(child.Text, language);
            TranslateControls(child, language);
        }
    }
}
