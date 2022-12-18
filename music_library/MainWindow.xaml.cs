using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Data.SQLite;
using System.Media;
using System.ComponentModel;
using System.Data.Entity;
using System.Windows.Threading;
using System.Windows.Resources;

namespace music_library
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string directItems = "";
		AppContext db;
		DispatcherTimer timer;
		List<int> countSongs = new List<int>();
		int idSong;
		bool MixSongs, InfiniteMus;

		public MainWindow()
		{
			InitializeComponent();
			db = new AppContext();
			db.Songs.Load();
			this.DataContext = db.Songs.Local.ToBindingList();
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(200);
			timer.Tick += new EventHandler(timer_Tick);
		}

		private void ScanFiles_Click(object sender, RoutedEventArgs e) //Сканирование файлов
		{
			try
			{
				var dialog = new CommonOpenFileDialog();
				dialog.IsFolderPicker = true;
				directItems = "";
				CommonFileDialogResult result = dialog.ShowDialog();
				if (result == CommonFileDialogResult.Ok)
				{
					directItems = dialog.FileName;
					MessageBox.Show(directItems);

				}
				string[] findFiles = Directory.GetFiles(directItems, "*.mp3", System.IO.SearchOption.AllDirectories);
				for (int i = 0; i < findFiles.Length; i++)
				{
					findFiles[i] = findFiles[i].Substring(findFiles[i].LastIndexOf('\\') + 1);
				}
				MusicList.ItemsSource = findFiles;
			}
			catch
			{ };
		}

		private void AddSongs_Click(object sender, RoutedEventArgs e) //Открытие формы
		{
			AddSongWpf addSongWpf = new AddSongWpf()
			{
				ShowInTaskbar = false,
				Owner = Application.Current.MainWindow
			};

			addSongWpf.SongPath.Text = directItems + @"\" + MusicList.SelectedValue;
			addSongWpf.ShowDialog();
			this.DataContext = addSongWpf.DataContext;
			countSongs.Clear();
			try
			{
				foreach (Song song in ListOfSongs.ItemsSource)
					countSongs.Add(song.id);
			}

			catch { };
		}

		public void playMusic(int id, string name, string creator, string path) //Функция кнопки play
		{
			mediaElement.Source = new Uri(Directory.GetCurrentDirectory() + path, UriKind.RelativeOrAbsolute);
			mediaElement.Play();
			idSong = id;
			NameTxt.Text = name;
			CreatorTxt.Text = creator;
		}

		private void PlayBtn_Click(object sender, RoutedEventArgs e) //Включение музыки
		{
			var id = (int)(sender as Button).Tag;
			idSong = id;
			countSongs.Clear();
			foreach (Song song in ListOfSongs.ItemsSource)
				countSongs.Add(song.id);
				

			foreach (Song song in ListOfSongs.ItemsSource)
			{
				if (id == song.id)
				{
					
					
					//MessageBox.Show(song.Path);
					//MessageBox.Show(Directory.GetCurrentDirectory());

					mediaElement.Source = new Uri(Directory.GetCurrentDirectory() + song.Path, UriKind.RelativeOrAbsolute);
					mediaElement.Play();
					NameTxt.Text = song.Name;
					CreatorTxt.Text = song.Creator;
				}
			}	
		}

		private void DeleteSongs_Click(object sender, RoutedEventArgs e) //удаление музыки
		{

			if (ListOfSongs.SelectedItem == null) MessageBox.Show("Выберите трек!", "Ошибка!");

			else
			{
				Song song = ListOfSongs.SelectedItem as Song;
				var result = MessageBox.Show($"Вы действительно хотите удалить трек: {song.Creator + "-" + song.Name}", "Внимание!", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					File.Delete(Directory.GetCurrentDirectory() + song.Path);
					db.Songs.Remove(song);
					db.SaveChanges();
					MessageBox.Show("Трек успешно удален!");
				}
				countSongs.Clear();
				foreach (Song songs in ListOfSongs.ItemsSource)
					countSongs.Add(songs.id);
			}
		}

		private void mediaElement_MediaOpened(object sender, RoutedEventArgs e) //Запуск таймера
		{
			if (mediaElement.NaturalDuration.HasTimeSpan)
			{
				TimeSpan ts = mediaElement.NaturalDuration.TimeSpan;
				TimeLineSlider.Maximum = ts.TotalSeconds;
				TimeLineSlider.SmallChange = 1;
				TimeLineSlider.LargeChange = Math.Min(10, ts.Seconds / 10);

				playMusBtn.Content = FindResource("Pause");
			}

			timer.Start();
			//TimeLineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
		}

		private void TimeLineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{

		}

		bool isDragging = false;

		void timer_Tick(object sender, EventArgs e) //Тик таймера
		{
			if(!isDragging)
			{
				TimeLineSlider.Value = mediaElement.Position.TotalSeconds;
				TimeSpan timeElapsed;
				timeElapsed = TimeSpan.FromSeconds(TimeLineSlider.Value);
				if (mediaElement.NaturalDuration.HasTimeSpan)
				{
					string vremya = string.Format("{0:D2}:{1:D2} - {2:D2}:{3:D2}", timeElapsed.Minutes, timeElapsed.Seconds, mediaElement.NaturalDuration.TimeSpan.Minutes, mediaElement.NaturalDuration.TimeSpan.Seconds);
					TimeAllTxt.Text = vremya;
				}
			}

			if (TimeLineSlider.Value == TimeLineSlider.Maximum)
			{
				this.NextMusicBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			}
		}

		private void TimeLineSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			isDragging = true;
		}

		private void TimeLineSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			isDragging = false;
			mediaElement.Position = TimeSpan.FromSeconds(TimeLineSlider.Value);
		}

		private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) //Регулятор громкости
		{
			mediaElement.Volume = VolumeSlider.Value;

			if(mediaElement.Volume >= 0.6)
			{
				volumeImg.Source = new BitmapImage(new Uri("/Resource/volumeHigh.png", UriKind.Relative));
			}

			else if (mediaElement.Volume == 0)
			{
				volumeImg.Source = new BitmapImage(new Uri("/Resource/volumeMute.png", UriKind.Relative));
			}

			else
			{
				volumeImg.Source = new BitmapImage(new Uri("/Resource/volumeMedium.png", UriKind.Relative));
			}
		}

		private void playMusBtn_Click(object sender, RoutedEventArgs e) //Кнопка Play
		{
			if (playMusBtn.Content == FindResource("Pause"))
			{
				playMusBtn.Content = FindResource("Play");
				mediaElement.Pause();
			}

			else if (playMusBtn.Content == FindResource("Play") && mediaElement.Source == null)
			{
				foreach (Song song in ListOfSongs.ItemsSource)
					countSongs.Add(song.id);
				foreach (Song song in ListOfSongs.ItemsSource)
				{
					if (countSongs.First() == song.id)
					{
						playMusic(song.id, song.Name, song.Creator, song.Path);
						return;
					}
				}
			}

			else if (playMusBtn.Content == FindResource("Play"))
			{
						playMusBtn.Content = FindResource("Pause");
						mediaElement.Play();
			}
		}

		private void BackMusicBtn_Click(object sender, RoutedEventArgs e) //Кнопка предыдущая песня
		{
			foreach (Song song in ListOfSongs.ItemsSource)
			{
				try
				{
					if (countSongs.Last(x => x < idSong) == song.id)
					{
						playMusic(song.id, song.Name, song.Creator, song.Path);
						return;
					}
				}

				catch
				{
					if (countSongs.Last() == song.id)
					{
						playMusic(song.id, song.Name, song.Creator, song.Path);
						return;
					}
				}
				
			}
		}

		private void NextMusicBtn_Click(object sender, RoutedEventArgs e) //Кнопка следующая песня
		{
			var randMus = new Random();
			foreach (Song song in ListOfSongs.ItemsSource)
			{

				try
				{
					if (InfiniteMus == true)
					{
						if(song.id == idSong)
						{
							playMusic(song.id, song.Name, song.Creator, song.Path);
							return;
						}
						
					}
				
					
					else if (MixSongs == true)
					{
						var randMusic = countSongs.Where(x => x != idSong);
						foreach (Song songs in ListOfSongs.ItemsSource)
						{
							if (randMusic.ElementAt(randMus.Next(0, randMusic.Count())) == songs.id)
							{
								playMusic(songs.id, songs.Name, songs.Creator, songs.Path);
								return;
							}
						}
					}

					else if (countSongs.First(x => x > idSong) == song.id)
					{
						playMusic(song.id, song.Name, song.Creator, song.Path);
						return;
					}
				}

				catch
				{
					if (countSongs.First() == song.id)
					{
						playMusic(song.id, song.Name, song.Creator, song.Path);
						return;
					}
				}
			}
		}

		private void MixSongBtn_Click(object sender, RoutedEventArgs e) //Кнопка случайной музыки
		{
			if (MixSongs == false)
			{
				MixSongs = true;
				MixSongBtn.Content = FindResource("shuffleOn");
				MixSongBtn.Background = new SolidColorBrush(Color.FromRgb(199, 200, 202));
				MixSongBtn.Background.Opacity = 0.45;
			}

			else
			{
				MixSongs = false;
				MixSongBtn.Background = new SolidColorBrush(Colors.Transparent);
				MixSongBtn.Background.Opacity = 1;
				MixSongBtn.Content = FindResource("shuffleOff");
			}
		}

		private void InfiniteSongBtn_Click(object sender, RoutedEventArgs e) //Бесконечная песня
		{
			if (InfiniteMus == false)
			{
				InfiniteMus = true;
				InfiniteSongBtn.Content = FindResource("recycleOn");
				InfiniteSongBtn.Background = new SolidColorBrush(Color.FromRgb(199, 200, 202));
				InfiniteSongBtn.Background.Opacity = 0.45;
			}
			
			else
			{
				InfiniteMus = false;
				InfiniteSongBtn.Background = new SolidColorBrush(Colors.Transparent);
				InfiniteSongBtn.Background.Opacity = 1;
				InfiniteSongBtn.Content = FindResource("recycleOff");
			}
		}
	}
}
