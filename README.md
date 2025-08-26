# KPT Viewer (WPF)

Небольшое WPF-приложение для быстрого парсинга и просмотра XML-выгрузки КПТ (кадастрового плана территории) в виде дерева объектов с возможностью поиска и экспорта выбранных узлов.

## Возможности

- Открытие XML через **File → Open** и **Drag&Drop** (бросить файл на окно).
- Дерево объектов слева, подробный **readonly** XML выбранного узла справа.
- Два поиска:
  - **по ID** (`cad_number`, `sk_id`, `reg_numb_border`);
  - **по адресу** (предпочтительно `readable_address`, иначе сборка текста из `<address>`).
  - Фильтр применяется на лету; сочетание полей — логическое **И**.
- Выбор объектов галочками и **Export** выбранных в новый XML, с сохранением «правильных» контейнеров.
- Копирование XML выбранного узла в буфер.
- Кросс-версия: из коробки **.NET 8 (WPF)**, легко портируется на **.NET Framework 4.7.2+**.

## Требования

- Windows 10/11
- .NET 8 SDK (или .NET Framework 4.7.2+ для портирования)
- Rider / Visual Studio 2022+

## Быстрый старт

```bash
# сборка
dotnet build

# запуск
dotnet run --project KptViewer.csproj
```

1. Open XML → выбери файл.

2. Слева появятся корни и объекты с уникальными ID.

3. Вводи запросы в полях ID и/или Адрес.

4. Отметь галочками нужные объекты → Save selected.

5. Справа можно Copy XML для текущего узла.

## Поддерживаемая структура XML

Корневые логические группы и контейнеры (ищутся по локальным именам; неймспейсы не мешают):

| Группа       | Контейнеры (вход)                                                   | Элементы (записи)                     | ID-поле для отображения |
| ------------ | ------------------------------------------------------------------- | ------------------------------------- | ----------------------- |
| Parcels      | `land_records`                                                      | `land_record`                         | `cad_number`            |
| ObjectRealty | `build_records`, `construction_records`                             | `build_record`, `construction_record` | `cad_number`            |
| SpatialData  | `spatial_data`                                                      | `entity_spatial`                      | `sk_id`                 |
| Bounds       | `municipal_boundaries`                                              | `municipal_boundary_record`           | `reg_numb_border`       |
| Zones        | `zones_and_territories_records`, `zones_and_territories_boundaries` | `zones_and_territories_record`        | `reg_numb_border`       |

**Адрес** берётся из <readable_address>…</readable_address>. Если его нет — собирается текст из <address>…</address> (листья).

## Архитектура

```
KptViewer
├─ Domain
│  ├─ RootDefinition (RootKind, containers, IdSelector)
│  ├─ ContainerSpec
│  ├─ NodeModel (DisplayId, Element, ContainerName, AddressText…)
│  ├─ IndexModel / RootItems
├─ Services
│  ├─ XmlIndexBuilder  (LINQ to XML → IndexModel; tolerant к разметке)
│  └─ ExportService    (группирует выбранные по реальному контейнеру и экспортирует)
├─ ViewModels
│  ├─ MainViewModel    (команды, загрузка, фильтр, export, help)
│  ├─ RootNodeVm / ItemNodeVm
│  └─ ViewModelBase
├─ UI
│  ├─ MainWindow.xaml  (TreeView + панель поиска + панель деталей)
│  └─ RelayCommand

```

### Как это работает

* XmlIndexBuilder обходит документ, ищет известные контейнеры и записи, формирует список NodeModel.

* Для каждого узла вычисляет:
  * DisplayId по функции IdSelector;
  * ContainerName по ближайшему предку (чтобы экспорт совпадал со входом);
  * AddressText.
  
* ExportService группирует выбранные по (ContainerName, ItemName) и кладёт копии элементов в новый XML под корнем <extract_cadastral_plan_territory>.
