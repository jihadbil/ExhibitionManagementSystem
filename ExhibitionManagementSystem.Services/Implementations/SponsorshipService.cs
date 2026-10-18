using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Services.Implementations;

public class SponsorshipService : ISponsorshipService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SponsorshipService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    #region Sponsors

    public async Task<ServiceResult<IList<SponsorDto>>> GetSponsorsAsync(int tenantId)
    {
        var sponsors = await _unitOfWork.Sponsors.AsQueryable()
            .Include(s => s.Contracts)
            .Where(s => s.TenantID == tenantId)
            .OrderBy(s => s.CompanyName)
            .ToListAsync();

        var dtos = _mapper.Map<IList<SponsorDto>>(sponsors);
        return ServiceResult<IList<SponsorDto>>.Success(dtos);
    }

    public async Task<ServiceResult<SponsorDto>> GetSponsorByIdAsync(int tenantId, int sponsorId)
    {
        var sponsor = await _unitOfWork.Sponsors.GetSponsorWithContractsAsync(tenantId, sponsorId);
        if (sponsor == null)
        {
            return ServiceResult<SponsorDto>.Failure("الراعي غير موجود", "SPONSOR_NOT_FOUND");
        }

        var dto = _mapper.Map<SponsorDto>(sponsor);
        return ServiceResult<SponsorDto>.Success(dto);
    }

    public async Task<ServiceResult<SponsorDto>> CreateSponsorAsync(int tenantId, SponsorCreateDto dto)
    {
        var sponsor = _mapper.Map<Sponsor>(dto);
        sponsor.TenantID = tenantId;
        sponsor.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Sponsors.AddAsync(sponsor);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<SponsorDto>(sponsor);
        return ServiceResult<SponsorDto>.Success(resultDto);
    }

    public async Task<ServiceResult<SponsorDto>> UpdateSponsorAsync(int tenantId, int sponsorId, SponsorUpdateDto dto)
    {
        var sponsor = await _unitOfWork.Sponsors.GetByIdAsync(sponsorId);
        if (sponsor == null || sponsor.TenantID != tenantId)
        {
            return ServiceResult<SponsorDto>.Failure("الراعي غير موجود", "SPONSOR_NOT_FOUND");
        }

        _mapper.Map(dto, sponsor);
        sponsor.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Sponsors.Update(sponsor);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<SponsorDto>(sponsor);
        return ServiceResult<SponsorDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeleteSponsorAsync(int tenantId, int sponsorId, string userId)
    {
        await _unitOfWork.BeginSerializableTransactionAsync();
        try
        {
            var result = await DeleteSponsorCoreAsync(tenantId, sponsorId, userId);
            if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
            return result;
        }
        catch (DbUpdateException)
        { return ServiceResult.Failure("تعارض حذف الراعي مع عملية أخرى", "SPONSOR_DELETE_CONFLICT"); }
        catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
        { return ServiceResult.Failure("تعارض حذف الراعي مع عملية أخرى", "SPONSOR_DELETE_CONFLICT"); }
        finally { await _unitOfWork.RollbackTransactionAsync(); }
    }

    private async Task<ServiceResult> DeleteSponsorCoreAsync(int tenantId, int sponsorId, string userId)
    {
        var sponsor = await _unitOfWork.Sponsors.GetByIdAsync(sponsorId);
        if (sponsor == null || sponsor.TenantID != tenantId)
        {
            return ServiceResult.Failure("الراعي غير موجود", "SPONSOR_NOT_FOUND");
        }

        if (await _unitOfWork.SponsorshipContracts.AsQueryable().IgnoreQueryFilters().AnyAsync(c =>
            c.TenantID == tenantId && c.SponsorID == sponsorId))
            return ServiceResult.Failure("لا يمكن حذف الراعي لوجود عقود مرتبطة به", "SPONSOR_HAS_CONTRACTS");

        await _unitOfWork.Sponsors.SoftDeleteAsync(sponsorId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    #endregion

    #region Packages

    public async Task<ServiceResult<IList<SponsorshipPackageDto>>> GetPackagesByExhibitionAsync(int tenantId, int exhibitionId)
    {
        var packages = await _unitOfWork.SponsorshipPackages.GetPackagesByExhibitionAsync(tenantId, exhibitionId);
        var dtos = _mapper.Map<IList<SponsorshipPackageDto>>(packages);
        return ServiceResult<IList<SponsorshipPackageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<SponsorshipPackageDto>> GetPackageByIdAsync(int tenantId, int packageId)
    {
        var package = await _unitOfWork.SponsorshipPackages.AsQueryable()
            .Include(p => p.Exhibition)
            .Include(p => p.Currency)
            .Include(p => p.Contracts)
            .FirstOrDefaultAsync(p => p.PackageID == packageId && p.TenantID == tenantId);

        if (package == null)
        {
            return ServiceResult<SponsorshipPackageDto>.Failure("باقة الرعاية غير موجودة", "PACKAGE_NOT_FOUND");
        }

        var dto = _mapper.Map<SponsorshipPackageDto>(package);
        return ServiceResult<SponsorshipPackageDto>.Success(dto);
    }

    public async Task<ServiceResult<SponsorshipPackageDto>> CreatePackageAsync(int tenantId, SponsorshipPackageCreateDto dto)
        => await RunMutationAsync(() => CreatePackageCoreAsync(tenantId, dto));

    private async Task<ServiceResult<SponsorshipPackageDto>> CreatePackageCoreAsync(int tenantId, SponsorshipPackageCreateDto dto)
    {
        var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
        if (exhibition == null || exhibition.TenantID != tenantId)
        {
            return ServiceResult<SponsorshipPackageDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        }

        var package = _mapper.Map<SponsorshipPackage>(dto);
        package.TenantID = tenantId;
        package.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.SponsorshipPackages.AddAsync(package);
        await _unitOfWork.SaveChangesAsync();

        var fullPackage = await _unitOfWork.SponsorshipPackages.AsQueryable()
            .Include(p => p.Exhibition)
            .Include(p => p.Currency)
            .Include(p => p.Contracts)
            .FirstOrDefaultAsync(p => p.PackageID == package.PackageID);

        var resultDto = _mapper.Map<SponsorshipPackageDto>(fullPackage ?? package);
        return ServiceResult<SponsorshipPackageDto>.Success(resultDto);
    }

    public async Task<ServiceResult<SponsorshipPackageDto>> UpdatePackageAsync(int tenantId, int packageId, SponsorshipPackageUpdateDto dto)
        => await RunMutationAsync(() => UpdatePackageCoreAsync(tenantId, packageId, dto));

    private async Task<ServiceResult<SponsorshipPackageDto>> UpdatePackageCoreAsync(int tenantId, int packageId, SponsorshipPackageUpdateDto dto)
    {
        var package = await _unitOfWork.SponsorshipPackages.GetByIdAsync(packageId);
        if (package == null || package.TenantID != tenantId)
        {
            return ServiceResult<SponsorshipPackageDto>.Failure("باقة الرعاية غير موجودة", "PACKAGE_NOT_FOUND");
        }

        var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
        if (exhibition == null || exhibition.TenantID != tenantId)
            return ServiceResult<SponsorshipPackageDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        if (dto.ExhibitionID != package.ExhibitionID && await _unitOfWork.SponsorshipContracts.AsQueryable()
            .IgnoreQueryFilters().AnyAsync(c => c.TenantID == tenantId && c.PackageID == packageId))
            return ServiceResult<SponsorshipPackageDto>.Failure("لا يمكن نقل باقة لها عقود إلى معرض آخر", "PACKAGE_HAS_CONTRACTS");
        var allocated = await _unitOfWork.SponsorshipContracts.AsQueryable().CountAsync(c =>
            c.TenantID == tenantId && c.PackageID == packageId &&
            c.Status != SponsorshipStatus.Cancelled && c.Status != SponsorshipStatus.Completed);
        if (dto.MaxSponsorsCount < 1 || dto.MaxSponsorsCount < allocated)
            return ServiceResult<SponsorshipPackageDto>.Failure("سعة الباقة لا يمكن أن تقل عن عدد العقود القائمة", "PACKAGE_CAPACITY_CONFLICT");

        _mapper.Map(dto, package);
        package.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.SponsorshipPackages.Update(package);
        await _unitOfWork.SaveChangesAsync();

        var fullPackage = await _unitOfWork.SponsorshipPackages.AsQueryable()
            .Include(p => p.Exhibition)
            .Include(p => p.Currency)
            .Include(p => p.Contracts)
            .FirstOrDefaultAsync(p => p.PackageID == packageId);

        var resultDto = _mapper.Map<SponsorshipPackageDto>(fullPackage ?? package);
        return ServiceResult<SponsorshipPackageDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeletePackageAsync(int tenantId, int packageId, string userId)
        => await RunMutationAsync(() => DeletePackageCoreAsync(tenantId, packageId, userId));

    private async Task<ServiceResult> DeletePackageCoreAsync(int tenantId, int packageId, string userId)
    {
        var package = await _unitOfWork.SponsorshipPackages.GetByIdAsync(packageId);
        if (package == null || package.TenantID != tenantId)
        {
            return ServiceResult.Failure("باقة الرعاية غير موجودة", "PACKAGE_NOT_FOUND");
        }

        if (await _unitOfWork.SponsorshipContracts.AsQueryable().IgnoreQueryFilters()
            .AnyAsync(c => c.TenantID == tenantId && c.PackageID == packageId))
            return ServiceResult.Failure("لا يمكن حذف باقة مرتبطة بعقود رعاية أو تاريخ عقود", "PACKAGE_HAS_CONTRACTS");

        await _unitOfWork.SponsorshipPackages.SoftDeleteAsync(packageId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    #endregion

    #region Advertising Spaces

    public async Task<ServiceResult<IList<AdvertisingSpaceDto>>> GetSpacesByExhibitionAsync(int tenantId, int exhibitionId, bool? availableOnly = null)
    {
        var spaces = await _unitOfWork.AdvertisingSpaces.GetSpacesByExhibitionAsync(tenantId, exhibitionId, availableOnly);
        var dtos = _mapper.Map<IList<AdvertisingSpaceDto>>(spaces);
        return ServiceResult<IList<AdvertisingSpaceDto>>.Success(dtos);
    }

    public async Task<ServiceResult<AdvertisingSpaceDto>> GetSpaceByIdAsync(int tenantId, int spaceId)
    {
        var space = await _unitOfWork.AdvertisingSpaces.AsQueryable()
            .Include(a => a.Exhibition)
            .Include(a => a.Venue)
            .Include(a => a.Hall)
            .Include(a => a.Currency)
            .Include(a => a.AdAssignments)
                .ThenInclude(aa => aa.Contract)
                    .ThenInclude(c => c.Sponsor)
            .FirstOrDefaultAsync(a => a.SpaceID == spaceId && a.TenantID == tenantId);

        if (space == null)
        {
            return ServiceResult<AdvertisingSpaceDto>.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        var dto = _mapper.Map<AdvertisingSpaceDto>(space);
        return ServiceResult<AdvertisingSpaceDto>.Success(dto);
    }

    public async Task<ServiceResult<AdvertisingSpaceDto>> CreateSpaceAsync(int tenantId, AdvertisingSpaceCreateDto dto)
        => await RunMutationAsync(() => CreateSpaceCoreAsync(tenantId, dto));

    private async Task<ServiceResult<AdvertisingSpaceDto>> CreateSpaceCoreAsync(int tenantId, AdvertisingSpaceCreateDto dto)
    {
        var locationError = await ValidateSpaceLocationAsync(tenantId, dto);
        if (locationError != null)
            return ServiceResult<AdvertisingSpaceDto>.Failure(locationError.Value.Message, locationError.Value.Code);

        var existingCode = await _unitOfWork.AdvertisingSpaces.FirstOrDefaultAsync(
            a => a.ExhibitionID == dto.ExhibitionID && a.SpaceCode == dto.SpaceCode);

        if (existingCode != null)
        {
            return ServiceResult<AdvertisingSpaceDto>.Failure("رمز المساحة الإعلانية مستخدم بالفعل في هذا المعرض", "DUPLICATE_SPACE_CODE");
        }

        var space = _mapper.Map<AdvertisingSpace>(dto);
        space.TenantID = tenantId;
        space.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.AdvertisingSpaces.AddAsync(space);
        await _unitOfWork.SaveChangesAsync();

        var fullSpace = await _unitOfWork.AdvertisingSpaces.AsQueryable()
            .Include(a => a.Exhibition)
            .Include(a => a.Venue)
            .Include(a => a.Hall)
            .Include(a => a.Currency)
            .FirstOrDefaultAsync(a => a.SpaceID == space.SpaceID);

        var resultDto = _mapper.Map<AdvertisingSpaceDto>(fullSpace ?? space);
        return ServiceResult<AdvertisingSpaceDto>.Success(resultDto);
    }

    public async Task<ServiceResult<AdvertisingSpaceDto>> UpdateSpaceAsync(int tenantId, int spaceId, AdvertisingSpaceUpdateDto dto)
        => await RunMutationAsync(() => UpdateSpaceCoreAsync(tenantId, spaceId, dto));

    private async Task<ServiceResult<AdvertisingSpaceDto>> UpdateSpaceCoreAsync(int tenantId, int spaceId, AdvertisingSpaceUpdateDto dto)
    {
        var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(spaceId);
        if (space == null || space.TenantID != tenantId)
        {
            return ServiceResult<AdvertisingSpaceDto>.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        var locationError = await ValidateSpaceLocationAsync(tenantId, dto);
        if (locationError != null)
            return ServiceResult<AdvertisingSpaceDto>.Failure(locationError.Value.Message, locationError.Value.Code);
        var hasAssignments = await _unitOfWork.SponsorAdAssignments.AsQueryable().IgnoreQueryFilters().AnyAsync(a =>
            a.SpaceID == spaceId && a.Contract.TenantID == tenantId);
        if (dto.ExhibitionID != space.ExhibitionID && hasAssignments)
            return ServiceResult<AdvertisingSpaceDto>.Failure("لا يمكن نقل مساحة لها تخصيصات إلى معرض آخر", "SPACE_HAS_ASSIGNMENTS");
        if (dto.IsAvailable && await _unitOfWork.SponsorAdAssignments.ExistsAsync(a => a.SpaceID == spaceId))
            return ServiceResult<AdvertisingSpaceDto>.Failure("المساحة مخصصة لعقد قائم؛ ألغ التخصيص أولاً", "SPACE_ASSIGNMENT_CONFLICT");

        _mapper.Map(dto, space);
        space.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AdvertisingSpaces.Update(space);
        await _unitOfWork.SaveChangesAsync();

        var fullSpace = await _unitOfWork.AdvertisingSpaces.AsQueryable()
            .Include(a => a.Exhibition)
            .Include(a => a.Venue)
            .Include(a => a.Hall)
            .Include(a => a.Currency)
            .FirstOrDefaultAsync(a => a.SpaceID == spaceId);

        var resultDto = _mapper.Map<AdvertisingSpaceDto>(fullSpace ?? space);
        return ServiceResult<AdvertisingSpaceDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeleteSpaceAsync(int tenantId, int spaceId, string userId)
        => await RunMutationAsync(() => DeleteSpaceCoreAsync(tenantId, spaceId, userId));

    private async Task<ServiceResult> DeleteSpaceCoreAsync(int tenantId, int spaceId, string userId)
    {
        var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(spaceId);
        if (space == null || space.TenantID != tenantId)
        {
            return ServiceResult.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        if (await _unitOfWork.SponsorAdAssignments.AsQueryable().IgnoreQueryFilters()
            .AnyAsync(a => a.SpaceID == spaceId && a.Contract.TenantID == tenantId))
            return ServiceResult.Failure("لا يمكن حذف مساحة مرتبطة بتخصيصات عقود أو تاريخ تخصيصات", "SPACE_HAS_ASSIGNMENTS");

        await _unitOfWork.AdvertisingSpaces.SoftDeleteAsync(spaceId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    #endregion

    #region Contracts

    public async Task<ServiceResult<IList<SponsorshipContractDto>>> GetContractsByExhibitionAsync(int tenantId, int exhibitionId)
    {
        var contracts = await _unitOfWork.SponsorshipContracts.GetContractsByExhibitionAsync(tenantId, exhibitionId);
        var dtos = _mapper.Map<IList<SponsorshipContractDto>>(contracts);
        return ServiceResult<IList<SponsorshipContractDto>>.Success(dtos);
    }

    public async Task<ServiceResult<SponsorshipContractDto>> GetContractByIdAsync(int tenantId, int contractId)
    {
        var contract = await _unitOfWork.SponsorshipContracts.GetContractWithDetailsAsync(tenantId, contractId);
        if (contract == null)
        {
            return ServiceResult<SponsorshipContractDto>.Failure("عقد الرعاية غير موجود", "CONTRACT_NOT_FOUND");
        }

        var dto = _mapper.Map<SponsorshipContractDto>(contract);
        return ServiceResult<SponsorshipContractDto>.Success(dto);
    }

    public async Task<ServiceResult<SponsorshipContractDto>> CreateContractAsync(int tenantId, string userId, SponsorshipContractCreateDto dto)
        => await RunMutationAsync(() => CreateContractCoreAsync(tenantId, userId, dto));

    private async Task<ServiceResult<SponsorshipContractDto>> CreateContractCoreAsync(int tenantId, string userId, SponsorshipContractCreateDto dto)
    {
        if (dto.TotalAmount < 0)
        {
            return ServiceResult<SponsorshipContractDto>.Failure(
                "قيمة العقد لا يمكن أن تكون سالبة",
                "INVALID_CONTRACT_AMOUNT");
        }

        var sponsor = await _unitOfWork.Sponsors.GetByIdAsync(dto.SponsorID);
        if (sponsor == null || sponsor.TenantID != tenantId)
        {
            return ServiceResult<SponsorshipContractDto>.Failure("الراعي غير موجود", "SPONSOR_NOT_FOUND");
        }

        var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
        if (exhibition == null || exhibition.TenantID != tenantId)
        {
            return ServiceResult<SponsorshipContractDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        }

        if (dto.PackageID.HasValue)
        {
            var package = await _unitOfWork.SponsorshipPackages.AsQueryable()
                .FirstOrDefaultAsync(p =>
                    p.PackageID == dto.PackageID.Value &&
                    p.TenantID == tenantId &&
                    p.ExhibitionID == dto.ExhibitionID);
            if (package == null)
            {
                return ServiceResult<SponsorshipContractDto>.Failure(
                    "باقة الرعاية غير موجودة في هذا المعرض",
                    "PACKAGE_NOT_FOUND");
            }
            if (!package.IsActive)
                return ServiceResult<SponsorshipContractDto>.Failure("باقة الرعاية غير نشطة", "PACKAGE_INACTIVE");
            if (dto.Status != SponsorshipStatus.Cancelled && dto.Status != SponsorshipStatus.Completed)
            {
                var allocated = await _unitOfWork.SponsorshipContracts.AsQueryable().CountAsync(c =>
                    c.TenantID == tenantId && c.PackageID == package.PackageID &&
                    c.Status != SponsorshipStatus.Cancelled && c.Status != SponsorshipStatus.Completed);
                if (allocated >= package.MaxSponsorsCount)
                    return ServiceResult<SponsorshipContractDto>.Failure("باقة الرعاية مكتملة السعة", "PACKAGE_CAPACITY_CONFLICT");
            }
        }

        var requestedSpaceIds = dto.SpaceIDsToAssign
            .Distinct()
            .ToArray();
        if (!Enum.IsDefined(dto.Status) ||
            (dto.Status is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed && requestedSpaceIds.Length > 0))
            return ServiceResult<SponsorshipContractDto>.Failure("حالة العقد لا تقبل تخصيص المساحات", "CONTRACT_CLOSED");
        var spacesToAssign = requestedSpaceIds.Length == 0
            ? []
            : await _unitOfWork.AdvertisingSpaces.AsQueryable()
                .Where(space =>
                    requestedSpaceIds.Contains(space.SpaceID) &&
                    space.TenantID == tenantId &&
                    space.ExhibitionID == dto.ExhibitionID &&
                    space.IsAvailable)
                .ToListAsync();

        if (spacesToAssign.Count != requestedSpaceIds.Length)
        {
            return ServiceResult<SponsorshipContractDto>.Failure(
                "واحدة أو أكثر من المساحات الإعلانية غير متاحة لهذا المعرض",
                "SPACE_UNAVAILABLE");
        }

        string contractNumber = dto.ContractNumber ?? $"SPN-{dto.ExhibitionID}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var contract = new SponsorshipContract
            {
                TenantID = tenantId,
                ExhibitionID = dto.ExhibitionID,
                SponsorID = dto.SponsorID,
                PackageID = dto.PackageID,
                ContractNumber = contractNumber,
                TotalAmount = dto.TotalAmount,
                CurrencyCode = dto.CurrencyCode,
                Status = dto.Status,
                SignedDate = DateTime.UtcNow,
                PaymentDueDate = dto.PaymentDueDate,
                SpecialTerms = dto.SpecialTerms,
                Notes = dto.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SponsorshipContracts.AddAsync(contract);
            await _unitOfWork.SaveChangesAsync();

            // Assign advertising spaces if specified
            if (spacesToAssign.Count > 0)
            {
                foreach (var space in spacesToAssign)
                {
                    space.IsAvailable = false;
                    _unitOfWork.AdvertisingSpaces.Update(space);

                    var assignment = new SponsorAdAssignment
                    {
                        ContractID = contract.ContractID,
                        SpaceID = space.SpaceID,
                        AssignedDate = DateTime.UtcNow,
                        AssetStatus = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.SponsorAdAssignments.AddAsync(assignment);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();

            var fullContract = await _unitOfWork.SponsorshipContracts.GetContractWithDetailsAsync(tenantId, contract.ContractID);
            var resultDto = _mapper.Map<SponsorshipContractDto>(fullContract ?? contract);
            return ServiceResult<SponsorshipContractDto>.Success(resultDto);
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResult<SponsorshipContractDto>.Failure("تعارض إنشاء العقد مع عملية أخرى", "SPONSORSHIP_CONFLICT");
        }
        catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResult<SponsorshipContractDto>.Failure("تعارض إنشاء العقد مع عملية أخرى", "SPONSORSHIP_CONFLICT");
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResult<SponsorshipContractDto>.Failure("تعذر إنشاء عقد الرعاية.", "CONTRACT_CREATION_FAILED");
        }
    }

    public async Task<ServiceResult<SponsorshipContractDto>> UpdateContractStatusAsync(int tenantId, int contractId, string status)
    {
        var contract = await _unitOfWork.SponsorshipContracts.GetByIdAsync(contractId);
        if (contract == null || contract.TenantID != tenantId)
        {
            return ServiceResult<SponsorshipContractDto>.Failure("عقد الرعاية غير موجود", "CONTRACT_NOT_FOUND");
        }

        if (!Enum.TryParse<SponsorshipStatus>(status, true, out var newStatus) || !Enum.IsDefined(newStatus))
        {
            return ServiceResult<SponsorshipContractDto>.Failure("حالة العقد غير صالحة", "INVALID_STATUS");
        }

        if (contract.Status is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed && newStatus != contract.Status)
            return ServiceResult<SponsorshipContractDto>.Failure("لا يمكن إعادة فتح عقد منتهٍ؛ أنشئ عقداً جديداً", "INVALID_CONTRACT_TRANSITION");
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            contract.Status = newStatus;
            contract.UpdatedAt = DateTime.UtcNow;
            if (newStatus is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed)
                await ReleaseAssignmentsAsync(tenantId, contractId, "System");
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResult<SponsorshipContractDto>.Failure("تعذر تحديث عقد الرعاية", "CONTRACT_UPDATE_FAILED");
        }

        var fullContract = await _unitOfWork.SponsorshipContracts.GetContractWithDetailsAsync(tenantId, contractId);
        var resultDto = _mapper.Map<SponsorshipContractDto>(fullContract ?? contract);
        return ServiceResult<SponsorshipContractDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeleteContractAsync(int tenantId, int contractId, string userId)
    {
        var contract = await _unitOfWork.SponsorshipContracts.GetByIdAsync(contractId);
        if (contract == null || contract.TenantID != tenantId)
        {
            return ServiceResult.Failure("عقد الرعاية غير موجود", "CONTRACT_NOT_FOUND");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await ReleaseAssignmentsAsync(tenantId, contractId, userId);
            await _unitOfWork.SponsorshipContracts.SoftDeleteAsync(contractId, userId);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ServiceResult.Failure("تعذر حذف عقد الرعاية", "CONTRACT_DELETE_FAILED");
        }
        return ServiceResult.Success();
    }

    #endregion

    #region Space Assignments

    private async Task ReleaseAssignmentsAsync(int tenantId, int contractId, string userId)
    {
        var assignments = await _unitOfWork.SponsorAdAssignments.AsQueryable().Include(a => a.Space)
            .Where(a => a.ContractID == contractId && a.Space.TenantID == tenantId).ToListAsync();
        foreach (var assignment in assignments)
        {
            assignment.Space.IsAvailable = true;
            await _unitOfWork.SponsorAdAssignments.SoftDeleteAsync(assignment.AssignmentID, userId);
        }
    }

    public async Task<ServiceResult<SponsorAdAssignmentDto>> AssignSpaceAsync(int tenantId, SponsorAdAssignmentCreateDto dto)
        => await RunMutationAsync(() => AssignSpaceCoreAsync(tenantId, dto));

    private async Task<ServiceResult<SponsorAdAssignmentDto>> AssignSpaceCoreAsync(int tenantId, SponsorAdAssignmentCreateDto dto)
    {
        var contract = await _unitOfWork.SponsorshipContracts.GetByIdAsync(dto.ContractID);
        if (contract == null || contract.TenantID != tenantId)
        {
            return ServiceResult<SponsorAdAssignmentDto>.Failure("عقد الرعاية غير موجود", "CONTRACT_NOT_FOUND");
        }

        if (contract.Status is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed)
            return ServiceResult<SponsorAdAssignmentDto>.Failure("العقد منتهٍ ولا يقبل تخصيص مساحات", "CONTRACT_CLOSED");

        var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(dto.SpaceID);
        if (space == null ||
            space.TenantID != tenantId ||
            space.ExhibitionID != contract.ExhibitionID)
        {
            return ServiceResult<SponsorAdAssignmentDto>.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        if (!space.IsAvailable)
        {
            return ServiceResult<SponsorAdAssignmentDto>.Failure(
                "المساحة الإعلانية مخصصة بالفعل",
                "SPACE_UNAVAILABLE");
        }

        space.IsAvailable = false;
        _unitOfWork.AdvertisingSpaces.Update(space);

        var assignment = new SponsorAdAssignment
        {
            ContractID = dto.ContractID,
            SpaceID = dto.SpaceID,
            AssignedDate = DateTime.UtcNow,
            ArtworkFileUrl = dto.ArtworkFileUrl,
            Notes = dto.Notes,
            AssetStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _unitOfWork.SponsorAdAssignments.AddAsync(assignment);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<SponsorAdAssignmentDto>.Failure(
                "تم تخصيص المساحة بعملية أخرى متزامنة",
                "SPACE_ASSIGNMENT_CONFLICT");
        }

        var fullAssignment = await _unitOfWork.SponsorAdAssignments.AsQueryable()
            .Include(a => a.Space)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID);

        var resultDto = _mapper.Map<SponsorAdAssignmentDto>(fullAssignment ?? assignment);
        return ServiceResult<SponsorAdAssignmentDto>.Success(resultDto);
    }

    public async Task<ServiceResult> RemoveSpaceAssignmentAsync(int tenantId, int assignmentId, string userId)
    {
        var assignment = await _unitOfWork.SponsorAdAssignments.AsQueryable()
            .Include(a => a.Space)
            .Include(a => a.Contract)
            .FirstOrDefaultAsync(a =>
                a.AssignmentID == assignmentId &&
                a.Space.TenantID == tenantId &&
                a.Contract.TenantID == tenantId);

        if (assignment == null)
        {
            return ServiceResult.Failure("التخصيص غير موجود", "ASSIGNMENT_NOT_FOUND");
        }

        if (assignment.Space != null)
        {
            assignment.Space.IsAvailable = true;
            _unitOfWork.AdvertisingSpaces.Update(assignment.Space);
        }

        await _unitOfWork.SponsorAdAssignments.SoftDeleteAsync(assignmentId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    #endregion

    private async Task<(string Message, string Code)?> ValidateSpaceLocationAsync(int tenantId, AdvertisingSpaceCreateDto dto)
    {
        var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
        if (exhibition == null || exhibition.TenantID != tenantId)
            return ("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        if (dto.VenueID.HasValue)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(dto.VenueID.Value);
            if (venue == null || venue.TenantID != tenantId) return ("الموقع غير موجود", "VENUE_NOT_FOUND");
            if (dto.VenueID != exhibition.VenueID)
                return ("الموقع المحدد لا يتبع هذا المعرض", "SPACE_LOCATION_INVALID");
        }
        if (dto.HallID.HasValue)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID.Value, h => h.Venue);
            if (hall?.Venue == null || hall.Venue.TenantID != tenantId)
                return ("القاعة غير موجودة", "HALL_NOT_FOUND");
            if (hall.VenueID != exhibition.VenueID)
                return ("القاعة لا تتبع موقع المعرض", "SPACE_LOCATION_INVALID");
        }
        return null;
    }

    private async Task<ServiceResult> RunMutationAsync(Func<Task<ServiceResult>> mutation)
    {
        await _unitOfWork.BeginSerializableTransactionAsync();
        try
        {
            var result = await mutation();
            if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
            return result;
        }
        catch (DbUpdateException)
        { return ServiceResult.Failure("تعارضت عملية الرعاية مع تعديل آخر؛ أعد تحميل البيانات", "SPONSORSHIP_CONFLICT"); }
        catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
        { return ServiceResult.Failure("تعارضت عملية الرعاية مع تعديل آخر؛ أعد تحميل البيانات", "SPONSORSHIP_CONFLICT"); }
        finally { await _unitOfWork.RollbackTransactionAsync(); }
    }

    private async Task<ServiceResult<T>> RunMutationAsync<T>(Func<Task<ServiceResult<T>>> mutation)
    {
        await _unitOfWork.BeginSerializableTransactionAsync();
        try
        {
            var result = await mutation();
            if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
            return result;
        }
        catch (DbUpdateException)
        { return ServiceResult<T>.Failure("تعارضت عملية الرعاية مع تعديل آخر؛ أعد تحميل البيانات", "SPONSORSHIP_CONFLICT"); }
        catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
        { return ServiceResult<T>.Failure("تعارضت عملية الرعاية مع تعديل آخر؛ أعد تحميل البيانات", "SPONSORSHIP_CONFLICT"); }
        finally { await _unitOfWork.RollbackTransactionAsync(); }
    }
}
