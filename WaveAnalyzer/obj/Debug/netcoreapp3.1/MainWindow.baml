   M S B A M L   `   `   `  ����G  CWaveAnalyzer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:WaveAnalyzerWaveAnalyzer  5   	       WaveAnalyzer.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   ,localclr-namespace:WaveAnalyzer     5   	   -   5        $
��root��5   	     � Title$  Wave Analyzer��5   	   $	��720��6   $
��1280��6,   .��5	      �� .��5
   
   �� .+�5      � $��50, 25, 50, 25q�6   .+�5      ��  �� Orientation$ 
Horizontal=�6   .+�5      �� -   5         ��Name$ 
OpenButton��5      .��5      �� -   5         ��Name$ OpenIcon��5       5      �� 5      -   5        $ 
SaveButton��5      ${�False��6E   .��5      �� -   5        $ SaveIcon��5       5      �� 5      $��50, 0, 0, 0q�6   .+�5      9� -   5         9�Name$ 	DFTButton��5   #   ��5      �� 5   "    �� Bullet 5   &   �� 5   *   -   5        $ DFTIcon��5   0   5   #   .��5      �� ���#EEEEEE.+�5   "   �� -   5        $
 Hann��5   8   $
��Hann��6)   ${�False��6Y   �� 5   "   -	   5        $ Triang��5   <   $��Triangle��6)   ${�False��6a   5      5       5!      �� 5"      -
   5        $ FilterButton��5"   3   $��100, 0, 0, 0q�6   ${�False��6_   .��5#      �� -   5        $ 
FilterIcon��5#       5$      5%      �� 5&      $ 
Horizontal=�6   $��Center=�66   .+�5'      �� -   5        $ PlayPauseButton��5'      ${�False��6O   .��5(      �� -   5        $ PlayPauseIcon��5(       5)      �� 5*      -   5        $ 
StopButton��5*      ${�False��6E   .��5+      �� -   5        $ StopIcon��5+       5,      �� 5-      -   5        $ RecordButton��5-      .��5.      �� -   5        $ 
RecordIcon��5.       5/      50      �� 51      $ 
Horizontal=�6   $��Right=�66   .+�52      �� -   5        $��SampleEntry��52   3   $��0, 0, 25, 0q�6   $��22050��6F   $��20(�6S   $��30��6a    �� TextAlignment$ Center=�6p    �� TextWrapping$
 Wrap=�6�   $��80��6�   �� 53      -   5        $ ClearButton��53   2   $��50, 0, 0, 0q�6   .��54      �� -   5        $ 	ClearIcon��54       55      56      57      � 58      $��0, 50, 0, 0q�6    � ColumnDefinitions 59      �� 5:      $
��auto �6'   �� 5;      .+�5=      �� -   5        	 ��Name$	 	ScalerBar��5=   g   $��0��6   
 � Minimum$
 0Q�6,    � Maximum$ 0Q�68   $��10, 10, 10, 70q�6D   $��50��6\   6z   �� 5>      -   5         ��Name$ 	HostPanel��5>   B   $��1��6   $��10, 0, 0, 0q�6-   .+�5?      7  1System.Windows.Forms.Integration.WindowsFormsHost  -   5          Name$ LeftHost��5?   '   $��0, 40, 10, 0q�69     5@      -   5        $ 	RightHost��5@   '   $��0, -10, 10, 0q�6:   �� 5A      -   5        $	 WaveScroller��5A   8   $
 0Q�6    $ 0Q�6,   $��40, 0, 50, 0q�6N    �� Orientation$ 
Horizontal=�6d   $��50��6}   6�     5B      -   5        $ DFTHost��5B   '   $��0, 20, 10, 0q�68   5C      5D      5E      5F      5G      