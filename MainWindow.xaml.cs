using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;

namespace VideoToAudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string videoFilePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            List<DriveInfo> driveList = GetDriveList();

            if (driveList.Count > 0)
            {
                AddDriveInfo(driveList);
            }
        }

        /// <summary>
        /// 모든 드라이브 목록을 ComboBox Item List에 매핑하기 
        /// ComboBox Init-Index 0 : 선택
        /// </summary>
        /// <param name="drives"></param>
        private void AddDriveInfo(List<DriveInfo> drives) 
        {
            List<string> strDriveList = new List<string>();

            foreach (DriveInfo drive in drives)
            {
                strDriveList.Add(drive.Name);
            }

            strDriveList.Insert(0, "선택");
            cbDrive.ItemsSource = strDriveList;
            cbDrive.SelectedIndex = 0;
        }

        /// <summary>
        /// 사용자가 Audio 파일로 변환할 Video 파일의 경로를 가져온다.
        /// 파일은 단일 선택만 가능.
        /// 파일 Dialog에서 확인할 수 있는 확장자는 .mp4 .avi .mkv .wmv .flv .mov .mpeg 이다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFindVideo_Click(object sender, RoutedEventArgs e)
        {
            videoFilePath = string.Empty;

            // 폴더 선택 대화 상자를 열고, 사용자가 폴더를 선택하도록 합니다.
            var openFileDialog = new OpenFileDialog
            {
                Title = "동영상 파일 열기",
                Multiselect = false, // 단일 파일 선택
                Filter = "Video Files|*.mp4;*.avi;*.mkv;*.wmv;*.flv;*.mov;*.mpeg|All Files|*.*",
                FilterIndex = 1 
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // 선택된 비디오 파일들의 경로를 가져옵니다.
                string[] selectedFilePaths = openFileDialog.FileNames;

                // 선택된 파일들을 출력합니다.
                if (selectedFilePaths.Length > 0)
                {
                    string fileListString = string.Join("\n", selectedFilePaths);

                    // 파일 저장하기
                    videoFilePath = fileListString;
                }
                else
                {
                    MessageBox.Show("파일 정보를 가져올 수 없습니다.", "Video 파일 열기");
                }
            }


            // 버튼 색깔 바꾸기
            Border? borderElement = btnFindVideo.Template.FindName("borderBtnFindVideo", btnFindVideo) as Border;

            if (borderElement != null)
            {
                Color color;

                if (!string.IsNullOrEmpty(videoFilePath))
                {
                    color = (Color)ColorConverter.ConvertFromString("#9ACD32");
                    borderElement.Background = new SolidColorBrush(color);
                }
                else
                {
                    color = (Color)ColorConverter.ConvertFromString("#FFF2BE80");
                    borderElement.Background = new SolidColorBrush(color);
                }
            }
        }

        /// <summary>
        /// 시스템에 있는 모든 드라이브 목록 가져오기
        /// </summary>
        /// <returns></returns>
        private List<DriveInfo> GetDriveList()
        {
            List<DriveInfo> drives = new List<DriveInfo>();
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo driveInfo in allDrives)
            {
                drives.Add(driveInfo);
            }

            return drives;
        }

        /// <summary>
        /// Audio 파일 변환하기 버튼 클릭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cbDrive.SelectedIndex == 0)
            {
                MessageBox.Show("드라이브를 선택해주세요.", "파일 저장");
            }
            else
            {
                if (string.IsNullOrEmpty(videoFilePath))
                {
                    MessageBox.Show("먼저 변환할 Video 파일을 선택해주세요.", "Video 파일 열기");
                }
                else 
                {
                    // 커서 Wait 처리
                    Mouse.OverrideCursor = Cursors.Wait;

                    string directoryPath = string.Format("{0}audio\\downloads", cbDrive.SelectedValue.ToString());  // 디렉터리 경로
                    string videoFileName = videoFilePath.Split('\\')[videoFilePath.Split('\\').Length - 1].Split('.')[0]; // 비디오 파일명
                    string outputAudioFilePath = string.Format("{0}audio\\downloads\\{1}_audio_{2}.mp3", cbDrive.SelectedValue, videoFileName, DateTime.Now.ToString("yyyyMMddHHmmss")); // .mp3 파일 경로


                    // 디렉터리 존재 여부
                    if (!Directory.Exists(directoryPath)) 
                    {
                        // 디렉토리 생성
                        Directory.CreateDirectory(directoryPath);
                    }

                        
                    if (ExtractAudioFromVideo(videoFilePath, outputAudioFilePath))
                    {
                        //MessageBoxResult result = MessageBox.Show("파일 변환이 완료되었습니다.", "Audio 파일 변환하기", MessageBoxButton.OKCancel);

                        //if (result == MessageBoxResult.OK) 
                        //{
                        //    Clipboard.SetText(directoryPath);
                        //    MessageBox.Show(string.Format("저장 경로는 {0}입니다.\n경로 복사되었습니다.", directoryPath), "알림");
                        //}

                        Clipboard.SetText(directoryPath);
                        MessageBox.Show(string.Format("파일 변환이 완료되었습니다.\n저장 경로는 {0}입니다.\n경로 복사되었습니다.", directoryPath), "Audio 파일 변환하기");
                    }
                    else 
                    {
                        MessageBox.Show("파일 변환이 실패하였습니다.", "Audio 파일 변환하기");
                    }

                    // 커서 원래대로 복원
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// NAudio NuGet 패키지를 이용하여 Video 파일을 Audio 파일로 변환하기
        /// </summary>
        /// <param name="videoFilePath">Video 파일 경로</param>
        /// <param name="outputMp3Path">저장할 .mp3 경로</param>
        /// <returns></returns>
        private bool ExtractAudioFromVideo(string videoFilePath, string outputMp3Path)
        {
            try
            {
                using (var reader = new MediaFoundationReader(videoFilePath))
                {
                    // NAudio를 사용하여 오디오 스트림을 .mp3 파일로 저장
                    WaveFileWriter.CreateWaveFile(outputMp3Path, reader);
                }

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
            
        }

        /// <summary>
        /// 푸터 프로그램 문의 이메일 클릭 시 주소 복사하기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = (Hyperlink)sender;

            Clipboard.SetText(hyperlink.Inlines.OfType<Run>().FirstOrDefault()?.Text);

            MessageBox.Show("이메일 주소가 복사되었습니다.", "알림");
        }
    }
}
