# Home/Index Page - Localization Variables Documentation

This document provides a comprehensive reference for all localization variables used in the Home/Index page, organized by sections for easy maintenance and reference.

## Variable Naming Convention

All variables follow the pattern: `[SECTION]_[DESCRIPTION]`

- **SECTION**: Indicates which part of the page (HEADER, STATS, CHART, etc.)
- **DESCRIPTION**: Brief description of the content in UPPERCASE_SNAKE_CASE

## Variable Structure

### 📋 Header Section
**Prefix: `HEADER_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `HEADER_TITLE` | Monitoring & Evaluation Dashboard | لوحة معلومات المراقبة والتقييم | Main page title |
| `HEADER_SUBTITLE` | Comprehensive overview of development projects, performance metrics, and impact assessment | نظرة شاملة على مشاريع التنمية ومؤشرات الأداء وتقييم الأثر | Page subtitle/description |

### 📊 Statistics Cards Section
**Prefix: `STATS_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `STATS_ACTIVE_PROJECTS` | Active Projects | المشاريع النشطة | Statistics card label |
| `STATS_PARTNER_MINISTRIES` | Partner Ministries | الوزارات الشريكة | Statistics card label |
| `STATS_GOVERNORATES` | Governorates | المحافظات | Statistics card label |
| `STATS_FRAMEWORKS` | Frameworks | الأطر | Statistics card label |

### 📈 Charts Section
**Prefix: `CHART_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `CHART_PROJECTS_BY_MINISTRY` | Projects by Ministry | المشاريع حسب الوزارة | Chart title |
| `CHART_FRAMEWORK_PERFORMANCE` | Framework Performance | أداء الأطر | Chart title |
| `CHART_PROJECT_DISTRIBUTION_LOCATION` | Project Distribution by Location | توزيع المشاريع حسب الموقع | Chart title |
| `CHART_IMPLEMENTATION_PROGRESS_TREND` | Implementation Progress Trend | اتجاه تقدم التنفيذ | Chart title |
| `CHART_PERFORMANCE_PERCENTAGE` | Performance % | نسبة الأداء % | Chart data label |
| `CHART_PROJECT_IMPLEMENTATION` | Project Implementation | تنفيذ المشاريع | Chart data series label |
| `CHART_PERFORMANCE_INDICATORS` | Performance Indicators | مؤشرات الأداء | Chart data series label |

### 📋 Progress Section
**Prefix: `PROGRESS_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `PROGRESS_OVERVIEW_TITLE` | Implementation Progress Overview | نظرة عامة على تقدم التنفيذ | Section title |
| `PROGRESS_SDG_FRAMEWORK` | SDG Framework Implementation | تنفيذ إطار أهداف التنمية المستدامة | Progress bar label |
| `PROGRESS_NATIONAL_DEVELOPMENT_PLAN` | National Development Plan | خطة التنمية الوطنية | Progress bar label |
| `PROGRESS_MONITORING_EVALUATION_SYSTEM` | Monitoring & Evaluation System | نظام المراقبة والتقييم | Progress bar label |

### 🔔 Activity Section
**Prefix: `ACTIVITY_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `ACTIVITY_RECENT_TITLE` | Recent System Activity | النشاطات الحديثة للنظام | Section title |
| `ACTIVITY_NEW_PROJECT_ADDED` | New project "Education Infrastructure Development" added | تمت إضافة مشروع جديد "تطوير البنية التحتية للتعليم" | Activity message |
| `ACTIVITY_PERFORMANCE_INDICATORS_UPDATED` | Performance indicators updated for Health Sector | تم تحديث مؤشرات الأداء لقطاع الصحة | Activity message |
| `ACTIVITY_MONTHLY_REPORT_GENERATED` | Monthly monitoring report generated successfully | تم إنشاء تقرير المراقبة الشهري بنجاح | Activity message |
| `ACTIVITY_NEW_STAKEHOLDERS_ADDED` | 3 new stakeholders added to Water and Sanitation project | تمت إضافة 3 أصحاب مصلحة جدد لمشروع المياه والصرف الصحي | Activity message |
| `ACTIVITY_DASHBOARD_UPDATED` | Q3 performance dashboard updated | تم تحديث لوحة معلومات أداء الربع الثالث | Activity message |

### ⏰ Time Indicators
**Prefix: `TIME_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `TIME_2_HOURS_AGO` | 2 hours ago | منذ ساعتين | Time stamp |
| `TIME_5_HOURS_AGO` | 5 hours ago | منذ 5 ساعات | Time stamp |
| `TIME_1_DAY_AGO` | 1 day ago | منذ يوم واحد | Time stamp |
| `TIME_2_DAYS_AGO` | 2 days ago | منذ يومين | Time stamp |
| `TIME_3_DAYS_AGO` | 3 days ago | منذ 3 أيام | Time stamp |

### 📅 Month Names for Charts
**Prefix: `MONTH_`**

| Variable Name | English Value | Arabic Value | Usage |
|---------------|---------------|--------------|-------|
| `MONTH_JAN` | Jan | يناير | Chart x-axis label |
| `MONTH_FEB` | Feb | فبراير | Chart x-axis label |
| `MONTH_MAR` | Mar | مارس | Chart x-axis label |
| `MONTH_APR` | Apr | أبريل | Chart x-axis label |
| `MONTH_MAY` | May | مايو | Chart x-axis label |
| `MONTH_JUN` | Jun | يونيو | Chart x-axis label |
| `MONTH_JUL` | Jul | يوليو | Chart x-axis label |
| `MONTH_AUG` | Aug | أغسطس | Chart x-axis label |
| `MONTH_SEP` | Sep | سبتمبر | Chart x-axis label |
| `MONTH_OCT` | Oct | أكتوبر | Chart x-axis label |
| `MONTH_NOV` | Nov | نوفمبر | Chart x-axis label |
| `MONTH_DEC` | Dec | ديسمبر | Chart x-axis label |

## Usage Examples

### In Razor Views
```csharp
@Localizer["HEADER_TITLE"]
@Localizer["STATS_ACTIVE_PROJECTS"]
@Localizer["CHART_FRAMEWORK_PERFORMANCE"]
```

### In JavaScript (for charts)
```javascript
label: '@Localizer["CHART_PERFORMANCE_PERCENTAGE"]'
```

### In Controllers (if needed)
```csharp
var title = _localizer["HEADER_TITLE"];
```

## File Locations

- **English Resources**: `/Resources/Views/Home/Index.en.resx`
- **Arabic Resources**: `/Resources/Views/Home/Index.ar.resx`
- **View File**: `/Views/Home/Index.cshtml`

## Maintenance Notes

1. **Adding New Variables**: Follow the naming convention `[SECTION]_[DESCRIPTION]`
2. **Updating Content**: Ensure both English and Arabic files are updated simultaneously
3. **Testing**: Test both language versions to ensure proper rendering
4. **RTL Support**: Arabic content automatically supports right-to-left text direction

## Best Practices

1. **Consistency**: Use the same variable name across all resource files
2. **Descriptive Names**: Variable names should clearly indicate their purpose
3. **Grouping**: Keep related variables together with consistent prefixes
4. **Comments**: Add Arabic comments in the Arabic resource file for clarity
5. **Backup**: Keep backups before making major changes

---

*Last Updated: $(date)*
*Total Variables: 39*