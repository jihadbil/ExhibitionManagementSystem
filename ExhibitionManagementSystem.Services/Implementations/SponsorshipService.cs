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
            .ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);

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
        var sponsor = await _unitOfWork.Sponsors.GetByIdAsync(sponsorId);
        if (sponsor == null || sponsor.TenantID != tenantId)
        {
            return ServiceResult.Failure("الراعي غير موجود", "SPONSOR_NOT_FOUND");
        }

        if (await _unitOfWork.SponsorshipContracts.AsQueryable().AnyAsync(c => c.SponsorID == sponsorId, _unitOfWork.RequestCancellation))
            return ServiceResult.Failure("الراعي مرتبط بعقود محفوظة", "SPONSOR_HAS_CONTRACTS");
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
            .FirstOrDefaultAsync(p => p.PackageID == packageId && p.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

        if (package == null)
        {
            return ServiceResult<SponsorshipPackageDto>.Failure("باقة الرعاية غير موجودة", "PACKAGE_NOT_FOUND");
        }

        var dto = _mapper.Map<SponsorshipPackageDto>(package);
        return ServiceResult<SponsorshipPackageDto>.Success(dto);
    }

    public async Task<ServiceResult<SponsorshipPackageDto>> CreatePackageAsync(int tenantId, SponsorshipPackageCreateDto dto)
    {
        if (dto.MaxSponsorsCount < 1 || dto.Price < 0)
            return ServiceResult<SponsorshipPackageDto>.Failure("السعة والسعر غير صالحين", "INVALID_PACKAGE");
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
            .FirstOrDefaultAsync(p => p.PackageID == package.PackageID, cancellationToken: _unitOfWork.RequestCancellation);

        var resultDto = _mapper.Map<SponsorshipPackageDto>(fullPackage ?? package);
        return ServiceResult<SponsorshipPackageDto>.Success(resultDto);
    }

    public Task<ServiceResult<SponsorshipPackageDto>> UpdatePackageAsync(int tenantId, int packageId, SponsorshipPackageUpdateDto dto) => InTransactionAsync(async () =>
    {
        if (dto.MaxSponsorsCount < 1 || dto.Price < 0)
            return ServiceResult<SponsorshipPackageDto>.Failure("السعة والسعر غير صالحين", "INVALID_PACKAGE");
        if (!await _unitOfWork.Exhibitions.AsQueryable().AnyAsync(e => e.ExhibitionID == dto.ExhibitionID && e.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation))
            return ServiceResult<SponsorshipPackageDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        var package = await _unitOfWork.SponsorshipPackages.GetByIdAsync(packageId);
        if (package == null || package.TenantID != tenantId)
        {
            return ServiceResult<SponsorshipPackageDto>.Failure("باقة الرعاية غير موجودة", "PACKAGE_NOT_FOUND");
        }

        var allocated = await _unitOfWork.SponsorshipContracts.AsQueryable().CountAsync(c => c.PackageID == packageId && c.Status != SponsorshipStatus.Cancelled, cancellationToken: _unitOfWork.RequestCancellation);
        if (dto.MaxSponsorsCount < allocated || (dto.ExhibitionID != package.ExhibitionID
            && await _unitOfWork.SponsorshipContracts.AsQueryable().AnyAsync(c => c.PackageID == packageId, cancellationToken: _unitOfWork.RequestCancellation)))
            return ServiceResult<SponsorshipPackageDto>.Failure("التعديل يتعارض مع العقود الحالية", "PACKAGE_HAS_CONTRACTS");
        _mapper.Map(dto, package);
        package.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.SponsorshipPackages.Update(package);
        await _unitOfWork.SaveChangesAsync();

        var fullPackage = await _unitOfWork.SponsorshipPackages.AsQueryable()
            .Include(p => p.Exhibition)
            .Include(p => p.Currency)
            .Include(p => p.Contracts)
            .FirstOrDefaultAsync(p => p.PackageID == packageId, cancellationToken: _unitOfWork.RequestCancellation);

        var resultDto = _mapper.Map<SponsorshipPackageDto>(fullPackage ?? package);
        return ServiceResult<SponsorshipPackageDto>.Success(resultDto);
    });

    public Task<ServiceResult> DeletePackageAsync(int tenantId, int packageId, string userId) => InTransactionAsync(async () =>
    {
        var package = await _unitOfWork.SponsorshipPackages.GetByIdAsync(packageId);
        if (package == null || package.TenantID != tenantId)
        {
            return ServiceResult.Failure("باقة الرعاية غير موجودة", "PACKAGE_NOT_FOUND");
        }

        if (await _unitOfWork.SponsorshipContracts.AsQueryable().AnyAsync(c => c.PackageID == packageId, cancellationToken: _unitOfWork.RequestCancellation))
            return ServiceResult.Failure("الباقة مرتبطة بعقود محفوظة", "PACKAGE_HAS_CONTRACTS");
        await _unitOfWork.SponsorshipPackages.SoftDeleteAsync(packageId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    });

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
            .FirstOrDefaultAsync(a => a.SpaceID == spaceId && a.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation);

        if (space == null)
        {
            return ServiceResult<AdvertisingSpaceDto>.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        var dto = _mapper.Map<AdvertisingSpaceDto>(space);
        return ServiceResult<AdvertisingSpaceDto>.Success(dto);
    }

    public async Task<ServiceResult<AdvertisingSpaceDto>> CreateSpaceAsync(int tenantId, AdvertisingSpaceCreateDto dto)
    {
        if (!await ValidSpaceContextAsync(tenantId, dto))
            return ServiceResult<AdvertisingSpaceDto>.Failure("موقع المساحة الإعلانية غير صالح", "INVALID_SPACE_CONTEXT");
        var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
        if (exhibition == null || exhibition.TenantID != tenantId)
        {
            return ServiceResult<AdvertisingSpaceDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
        }

        var existingCode = await _unitOfWork.AdvertisingSpaces.FirstOrDefaultAsync(
            a => a.ExhibitionID == dto.ExhibitionID && a.SpaceCode == dto.SpaceCode, cancellationToken: _unitOfWork.RequestCancellation);

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
            .FirstOrDefaultAsync(a => a.SpaceID == space.SpaceID, cancellationToken: _unitOfWork.RequestCancellation);

        var resultDto = _mapper.Map<AdvertisingSpaceDto>(fullSpace ?? space);
        return ServiceResult<AdvertisingSpaceDto>.Success(resultDto);
    }

    public Task<ServiceResult<AdvertisingSpaceDto>> UpdateSpaceAsync(int tenantId, int spaceId, AdvertisingSpaceUpdateDto dto) => InTransactionAsync(async () =>
    {
        if (!await ValidSpaceContextAsync(tenantId, dto))
            return ServiceResult<AdvertisingSpaceDto>.Failure("موقع المساحة الإعلانية غير صالح", "INVALID_SPACE_CONTEXT");
        var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(spaceId);
        if (space == null || space.TenantID != tenantId)
        {
            return ServiceResult<AdvertisingSpaceDto>.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        var assigned = await _unitOfWork.SponsorAdAssignments.AsQueryable().AnyAsync(a => a.SpaceID == spaceId, cancellationToken: _unitOfWork.RequestCancellation);
        if (assigned && (dto.IsAvailable || dto.ExhibitionID != space.ExhibitionID
            || dto.HallID != space.HallID || dto.VenueID != space.VenueID))
            return ServiceResult<AdvertisingSpaceDto>.Failure("المساحة مخصصة حاليًا", "SPACE_ALREADY_ASSIGNED");
        _mapper.Map(dto, space);
        space.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AdvertisingSpaces.Update(space);
        await _unitOfWork.SaveChangesAsync();

        var fullSpace = await _unitOfWork.AdvertisingSpaces.AsQueryable()
            .Include(a => a.Exhibition)
            .Include(a => a.Venue)
            .Include(a => a.Hall)
            .Include(a => a.Currency)
            .FirstOrDefaultAsync(a => a.SpaceID == spaceId, cancellationToken: _unitOfWork.RequestCancellation);

        var resultDto = _mapper.Map<AdvertisingSpaceDto>(fullSpace ?? space);
        return ServiceResult<AdvertisingSpaceDto>.Success(resultDto);
    });

    public Task<ServiceResult> DeleteSpaceAsync(int tenantId, int spaceId, string userId) => InTransactionAsync(async () =>
    {
        var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(spaceId);
        if (space == null || space.TenantID != tenantId)
        {
            return ServiceResult.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        if (await _unitOfWork.SponsorAdAssignments.AsQueryable().AnyAsync(a => a.SpaceID == spaceId, cancellationToken: _unitOfWork.RequestCancellation))
            return ServiceResult.Failure("المساحة مخصصة حاليًا", "SPACE_ALREADY_ASSIGNED");
        await _unitOfWork.AdvertisingSpaces.SoftDeleteAsync(spaceId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    });

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
    {
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

        if (!Enum.IsDefined(dto.Status) || dto.TotalAmount < 0
            || dto.SpaceIDsToAssign == null || dto.SpaceIDsToAssign.Distinct().Count() != dto.SpaceIDsToAssign.Count)
            return ServiceResult<SponsorshipContractDto>.Failure("بيانات العقد غير صالحة", "INVALID_CONTRACT");
        string contractNumber = string.IsNullOrWhiteSpace(dto.ContractNumber) ? $"SPN-{Guid.NewGuid():N}" : dto.ContractNumber;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (dto.PackageID.HasValue && !await _unitOfWork.SponsorshipPackages.AsQueryable()
                .AnyAsync(p => p.PackageID == dto.PackageID && p.TenantID == tenantId && p.ExhibitionID == dto.ExhibitionID, cancellationToken: _unitOfWork.RequestCancellation))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<SponsorshipContractDto>.Failure("باقة الرعاية غير موجودة في المعرض", "PACKAGE_NOT_FOUND");
            }
            if (dto.PackageID.HasValue && dto.Status != SponsorshipStatus.Cancelled)
            {
                var package = await _unitOfWork.SponsorshipPackages.AsQueryable().AsNoTracking().SingleAsync(p => p.PackageID == dto.PackageID, cancellationToken: _unitOfWork.RequestCancellation);
                var allocated = await _unitOfWork.SponsorshipContracts.AsQueryable()
                    .CountAsync(c => c.PackageID == dto.PackageID && c.Status != SponsorshipStatus.Cancelled, cancellationToken: _unitOfWork.RequestCancellation);
                if (!package.IsActive || allocated >= package.MaxSponsorsCount)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResult<SponsorshipContractDto>.Failure("سعة باقة الرعاية مكتملة أو غير مفعلة", "PACKAGE_SOLD_OUT");
                }
            }
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
            if (dto.SpaceIDsToAssign != null && dto.SpaceIDsToAssign.Any())
            {
                foreach (var spaceId in dto.SpaceIDsToAssign)
                {
                    var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(spaceId);
                    if (space == null || space.TenantID != tenantId || space.ExhibitionID != dto.ExhibitionID)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<SponsorshipContractDto>.Failure("المساحة لا تنتمي إلى المعرض", "SPACE_NOT_FOUND");
                    }
                    if (!space.IsAvailable || dto.Status is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed
                        || await _unitOfWork.SponsorAdAssignments.AsQueryable().AnyAsync(a => a.SpaceID == spaceId, cancellationToken: _unitOfWork.RequestCancellation))
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResult<SponsorshipContractDto>.Failure("المساحة مخصصة أو العقد غير فعال", "SPACE_ALREADY_ASSIGNED");
                    }
                    if (space != null)
                    {
                        space.IsAvailable = false;
                        _unitOfWork.AdvertisingSpaces.Update(space);

                        var assignment = new SponsorAdAssignment
                        {
                            ContractID = contract.ContractID,
                            SpaceID = spaceId,
                            AssignedDate = DateTime.UtcNow,
                            AssetStatus = "Pending",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.SponsorAdAssignments.AddAsync(assignment);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();

            var fullContract = await _unitOfWork.SponsorshipContracts.GetContractWithDetailsAsync(tenantId, contract.ContractID);
            var resultDto = _mapper.Map<SponsorshipContractDto>(fullContract ?? contract);
            return ServiceResult<SponsorshipContractDto>.Success(resultDto);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public Task<ServiceResult<SponsorshipContractDto>> UpdateContractStatusAsync(int tenantId, int contractId, string status) => InTransactionAsync(async () =>
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

        if (contract.Status != newStatus && contract.Status is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed)
            return ServiceResult<SponsorshipContractDto>.Failure("لا يمكن إعادة تفعيل عقد منتهٍ أو ملغى", "INVALID_CONTRACT_TRANSITION");
        if (newStatus is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed)
            await ReleaseAssignmentsAsync(contractId);
        contract.Status = newStatus;
        contract.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.SponsorshipContracts.Update(contract);
        await _unitOfWork.SaveChangesAsync();

        var fullContract = await _unitOfWork.SponsorshipContracts.GetContractWithDetailsAsync(tenantId, contractId);
        var resultDto = _mapper.Map<SponsorshipContractDto>(fullContract ?? contract);
        return ServiceResult<SponsorshipContractDto>.Success(resultDto);
    });

    public Task<ServiceResult> DeleteContractAsync(int tenantId, int contractId, string userId) => InTransactionAsync(async () =>
    {
        var contract = await _unitOfWork.SponsorshipContracts.GetByIdAsync(contractId);
        if (contract == null || contract.TenantID != tenantId)
        {
            return ServiceResult.Failure("عقد الرعاية غير موجود", "CONTRACT_NOT_FOUND");
        }

        await ReleaseAssignmentsAsync(contractId, userId);
        await _unitOfWork.SponsorshipContracts.SoftDeleteAsync(contractId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    });

    #endregion

    #region Space Assignments

    public Task<ServiceResult<SponsorAdAssignmentDto>> AssignSpaceAsync(int tenantId, SponsorAdAssignmentCreateDto dto) => InTransactionAsync(async () =>
    {
        var contract = await _unitOfWork.SponsorshipContracts.GetByIdAsync(dto.ContractID);
        if (contract == null || contract.TenantID != tenantId)
        {
            return ServiceResult<SponsorAdAssignmentDto>.Failure("عقد الرعاية غير موجود", "CONTRACT_NOT_FOUND");
        }

        var space = await _unitOfWork.AdvertisingSpaces.GetByIdAsync(dto.SpaceID);
        if (space == null || space.TenantID != tenantId || space.ExhibitionID != contract.ExhibitionID)
        {
            return ServiceResult<SponsorAdAssignmentDto>.Failure("المساحة الإعلانية غير موجودة", "SPACE_NOT_FOUND");
        }

        if (contract.Status is SponsorshipStatus.Cancelled or SponsorshipStatus.Completed || !space.IsAvailable
            || await _unitOfWork.SponsorAdAssignments.AsQueryable().AnyAsync(a => a.SpaceID == dto.SpaceID, cancellationToken: _unitOfWork.RequestCancellation))
            return ServiceResult<SponsorAdAssignmentDto>.Failure("المساحة مخصصة أو العقد غير فعال", "SPACE_ALREADY_ASSIGNED");
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

        await _unitOfWork.SponsorAdAssignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        var fullAssignment = await _unitOfWork.SponsorAdAssignments.AsQueryable()
            .Include(a => a.Space)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID, cancellationToken: _unitOfWork.RequestCancellation);

        var resultDto = _mapper.Map<SponsorAdAssignmentDto>(fullAssignment ?? assignment);
        return ServiceResult<SponsorAdAssignmentDto>.Success(resultDto);
    });

    public Task<ServiceResult> RemoveSpaceAssignmentAsync(int tenantId, int assignmentId, string userId) => InTransactionAsync(async () =>
    {
        var assignment = await _unitOfWork.SponsorAdAssignments.AsQueryable()
            .Include(a => a.Space)
            .Include(a => a.Contract)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId, cancellationToken: _unitOfWork.RequestCancellation);

        if (assignment == null || assignment.Contract.TenantID != tenantId || assignment.Space.TenantID != tenantId)
        {
            return ServiceResult.Failure("التخصيص غير موجود", "ASSIGNMENT_NOT_FOUND");
        }

        if (assignment.Space != null)
        {
            assignment.Space.IsAvailable = !await _unitOfWork.SponsorAdAssignments.AsQueryable()
                .AnyAsync(a => a.SpaceID == assignment.SpaceID && a.AssignmentID != assignmentId, cancellationToken: _unitOfWork.RequestCancellation);
            _unitOfWork.AdvertisingSpaces.Update(assignment.Space);
        }

        await _unitOfWork.SponsorAdAssignments.SoftDeleteAsync(assignmentId, userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    });

    #endregion

    private async Task<bool> ValidSpaceContextAsync(int tenantId, AdvertisingSpaceCreateDto dto)
    {
        var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
        if (exhibition == null || exhibition.TenantID != tenantId) return false;
        if (dto.VenueID.HasValue && dto.VenueID != exhibition.VenueID) return false;
        if (dto.HallID.HasValue && !await _unitOfWork.Halls.AsQueryable()
            .AnyAsync(h => h.HallID == dto.HallID && h.VenueID == exhibition.VenueID && h.Venue.TenantID == tenantId, cancellationToken: _unitOfWork.RequestCancellation)) return false;
        return true;
    }

    private async Task ReleaseAssignmentsAsync(int contractId, string? userId = null)
    {
        var assignments = await _unitOfWork.SponsorAdAssignments.AsQueryable().Include(a => a.Space)
            .Where(a => a.ContractID == contractId).ToListAsync(cancellationToken: _unitOfWork.RequestCancellation);
        foreach (var assignment in assignments)
        {
            // Preserve the assignment history and free its physical resource.
            assignment.IsDeleted = true;
            assignment.DeletedAt = DateTime.UtcNow;
            assignment.DeletedByUserId = userId;
            assignment.Space.IsAvailable = true;
        }
    }

    private async Task<T> InTransactionAsync<T>(Func<Task<T>> action) where T : ServiceResult
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await action();
            if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
            else await _unitOfWork.RollbackTransactionAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
