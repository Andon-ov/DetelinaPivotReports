using System;
using DetelinaPivotReports.ViewModels;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Запис за детайлна справка продажби по терминали и бонове.
/// </summary>
public class DetailedSaleRecord : ViewModelBase
{
    private int _terminalId;
    private string _terminalName = string.Empty;
    private long _bonNumber;
    private DateTime _saleDateTime;
    private decimal _bonTotal;
    private string _groupName = string.Empty;
    private int _pluNumber;
    private string _articleName = string.Empty;
    private decimal _quantity;
    private decimal _unitPrice;
    private decimal _discount;
    private decimal _rowTotal;
    private long _sellId;

    private bool _isFirstInReceipt = true;
    private bool _isLastInReceipt = true;
    private bool _isAlternateReceiptGroup;

    public int TerminalId
    {
        get => _terminalId;
        set => SetProperty(ref _terminalId, value);
    }

    public string TerminalName
    {
        get => _terminalName;
        set
        {
            if (SetProperty(ref _terminalName, value))
                OnPropertyChanged(nameof(DisplayTerminal));
        }
    }

    public long BonNumber
    {
        get => _bonNumber;
        set
        {
            if (SetProperty(ref _bonNumber, value))
                OnPropertyChanged(nameof(DisplayBonNumber));
        }
    }

    public DateTime SaleDateTime
    {
        get => _saleDateTime;
        set
        {
            if (SetProperty(ref _saleDateTime, value))
                OnPropertyChanged(nameof(DisplaySaleDateTime));
        }
    }

    public decimal BonTotal
    {
        get => _bonTotal;
        set
        {
            if (SetProperty(ref _bonTotal, value))
                OnPropertyChanged(nameof(DisplayBonTotal));
        }
    }

    public string GroupName
    {
        get => _groupName;
        set
        {
            if (SetProperty(ref _groupName, value))
                OnPropertyChanged(nameof(DisplayGroupName));
        }
    }

    public int PluNumber
    {
        get => _pluNumber;
        set => SetProperty(ref _pluNumber, value);
    }

    public string ArticleName
    {
        get => _articleName;
        set => SetProperty(ref _articleName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set => SetProperty(ref _unitPrice, value);
    }

    public decimal Discount
    {
        get => _discount;
        set => SetProperty(ref _discount, value);
    }

    public decimal RowTotal
    {
        get => _rowTotal;
        set => SetProperty(ref _rowTotal, value);
    }

    public long SellId
    {
        get => _sellId;
        set => SetProperty(ref _sellId, value);
    }

    /// <summary>
    /// Истина, ако този ред е първи за съответния бон (за показване на Терминал, Бон No, Дата, Тотал, Група).
    /// </summary>
    public bool IsFirstInReceipt
    {
        get => _isFirstInReceipt;
        set
        {
            if (SetProperty(ref _isFirstInReceipt, value))
            {
                OnPropertyChanged(nameof(DisplayTerminal));
                OnPropertyChanged(nameof(DisplayBonNumber));
                OnPropertyChanged(nameof(DisplaySaleDateTime));
                OnPropertyChanged(nameof(DisplayBonTotal));
                OnPropertyChanged(nameof(DisplayGroupName));
            }
        }
    }

    /// <summary>
    /// Истина, ако този ред е последен за съответния бон (за долна разделителна линия между боновете).
    /// </summary>
    public bool IsLastInReceipt
    {
        get => _isLastInReceipt;
        set => SetProperty(ref _isLastInReceipt, value);
    }

    /// <summary>
    /// Истина, ако целият бон трябва да има алтернативния зебра фон (#F4F7FB).
    /// </summary>
    public bool IsAlternateReceiptGroup
    {
        get => _isAlternateReceiptGroup;
        set => SetProperty(ref _isAlternateReceiptGroup, value);
    }

    /// <summary>
    /// Уникален ключ на бона за групиране.
    /// </summary>
    public string ReceiptKey => SellId > 0 
        ? $"SELL_{SellId}" 
        : $"TERM_{TerminalId}_BON_{BonNumber}_{SaleDateTime:yyyyMMdd}";

    // Свойства за чисто визуализиране САМО на първия ред от всеки бон:
    public string DisplayTerminal => IsFirstInReceipt ? TerminalName : string.Empty;
    public string DisplayBonNumber => IsFirstInReceipt ? (BonNumber > 0 ? BonNumber.ToString() : string.Empty) : string.Empty;
    public string DisplaySaleDateTime => IsFirstInReceipt ? (SaleDateTime != DateTime.MinValue ? SaleDateTime.ToString("dd.MM.yyyy HH:mm:ss") : string.Empty) : string.Empty;
    public string DisplayBonTotal => IsFirstInReceipt ? BonTotal.ToString("#,##0.00") : string.Empty;
    public string DisplayGroupName => IsFirstInReceipt ? GroupName : string.Empty;
}
