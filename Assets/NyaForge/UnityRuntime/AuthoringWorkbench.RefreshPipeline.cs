namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Groups panel refresh calls by responsibility. The main workbench
        /// remains the order owner while each feature keeps its own panel
        /// implementation in a focused partial file.
        /// </summary>
        void RefreshEditingPanels()
        {
            RefreshFaceEditing();
            RefreshSolidify();
            RefreshUv();
            RefreshPaint();
            RefreshMaterials();
            RefreshRig();
            RefreshMorph();
        }

        void RefreshSimulationAndOutputPanels()
        {
            RefreshEvidenceCapture();
            RefreshShapeCreationPanel();
            RefreshModelImport();
            RefreshPhysBonesStatus();
            RefreshSecondaryMotionStatus();
            RefreshSpringPlayback();
            RefreshValidation();
            RefreshCommandBar();
        }
    }
}
