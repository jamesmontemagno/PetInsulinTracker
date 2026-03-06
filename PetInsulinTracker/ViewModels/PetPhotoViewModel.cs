using CommunityToolkit.Mvvm.ComponentModel;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class PetPhotoViewModel : ObservableObject
{
	private readonly IDatabaseService _db;

	public PetPhotoViewModel(IDatabaseService db)
	{
		_db = db;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private Pet? pet;

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadPetSafelyAsync(value);
	}

	private async Task LoadPetSafelyAsync(string id)
	{
		try
		{
			await LoadPetAsync(id);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to load pet photo: {ex.Message}");
		}
	}

	private async Task LoadPetAsync(string id)
	{
		Pet = await _db.GetPetAsync(id);
	}
}
