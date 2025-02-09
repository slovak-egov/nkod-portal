import { useEffect } from 'react';
import FormElementGroup from './FormElementGroup';
import MultiLanguageFormGroup from './MultiLanguageFormGroup';
import MultiRadio from './MultiRadio';
import { CodelistValue, Dataset, DatasetInput, UserInfo, extractLanguageErrors, knownCodelists, useCodelists, useDatasets } from '../client';
import BaseInput from './BaseInput';
import TextArea from './TextArea';
import MultiSelectElementItems from './MultiSelecteElementItems';
import SelectElementItems from './SelectElementItems';
import MultiTextBox from './MultiTextBox';
import MultiCheckbox from './MultiCheckbox';
import ErrorAlert from './ErrorAlert';
import Loading from './Loading';
import { useTranslation } from 'react-i18next';
import DateInput from './DateInput';
import Checkbox from './Checkbox';

type Props = {
    dataset: DatasetInput;
    setDataset: (properties: Partial<DatasetInput>) => void;
    saving: boolean;
    userInfo: UserInfo | null;
    errors: { [id: string]: string };
};

type PublicOption = {
    name: string;
    value: boolean;
};

const requiredCodelists = [
    knownCodelists.dataset.theme,
    knownCodelists.dataset.type,
    knownCodelists.dataset.accrualPeriodicity,
    knownCodelists.dataset.spatial,
    knownCodelists.dataset.hvdCategory
];

export function DatasetForm(props: Props) {
    const { t } = useTranslation();

    const [datasets, , setDatasetQueryParameters, loadingDatasets, errorDatasets] = useDatasets({
        pageSize: -1,
        page: 0
    });

    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);

    const { dataset, setDataset, userInfo, errors } = props;

    const publicOptions = [
        {
            name: t('published'),
            value: true
        },
        {
            name: t('notPublished'),
            value: false
        }
    ];

    useEffect(() => {
        if (userInfo?.publisher) {
            setDatasetQueryParameters({
                filters: {
                    publishers: [userInfo.publisher],
                    serie: ['1']
                },
                page: 1
            });
        }
    }, [userInfo, setDatasetQueryParameters]);

    const loading = loadingDatasets || loadingCodelists;
    const error = errorDatasets ?? errorCodelists;

    const typeCodelist = codelists.find((c) => c.id === knownCodelists.dataset.type);
    const themeCodelist = codelists.find((c) => c.id === knownCodelists.dataset.theme);
    const accrualPeriodicityCodelist = codelists.find((c) => c.id === knownCodelists.dataset.accrualPeriodicity);
    const spatialCodelist = codelists.find((c) => c.id === knownCodelists.dataset.spatial);
    const hvdCategoryCodelist = codelists.find((c) => c.id === knownCodelists.dataset.hvdCategory);

    const isHvd = dataset.type.includes('http://publications.europa.eu/resource/authority/dataset-type/HVD');

    const saving = props.saving;

    const isEnabledPartOf = datasets && datasets.items.length > 0;

    return (
        <>
            {loading ? <Loading /> : null}
            {error ? <ErrorAlert error={error} /> : null}

            <MultiRadio<PublicOption>
                label={t('datasetState')}
                inline
                options={publicOptions}
                id="public-selection"
                getValue={(v) => v.name}
                renderOption={(v) => v.name}
                selectedOption={publicOptions.find((o) => o.value === dataset.isPublic) ?? publicOptions[0]}
                onChange={(o) => setDataset({ ...dataset, isPublic: o.value })}
            />

            {typeCodelist ? (
                <FormElementGroup
                    label={t('datasetType')}
                    element={(id) => (
                        <MultiCheckbox<CodelistValue>
                            id={id}
                            options={typeCodelist.values}
                            selectedValues={dataset.type}
                            getLabel={(v) => v.label}
                            getValue={(v) => v.id}
                            onCheckedChanged={(v) => {
                                setDataset({ ...dataset, type: v });
                            }}
                        />
                    )}
                />
            ) : null}

            <MultiLanguageFormGroup<string>
                label={t('datasetName')}
                values={dataset.name}
                onChange={(v) => setDataset({ name: v })}
                emptyValue=""
                errorMessage={extractLanguageErrors(errors, 'name')}
                element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
            />
            <MultiLanguageFormGroup<string>
                label={t('description')}
                values={dataset.description}
                onChange={(v) => setDataset({ description: v })}
                emptyValue=""
                errorMessage={extractLanguageErrors(errors, 'description')}
                element={(id, value, onChange) => <TextArea id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
            />

            {themeCodelist ? (
                <FormElementGroup
                    label={t('theme')}
                    errorMessage={errors['themes']}
                    element={(id) => (
                        <MultiSelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={themeCodelist.values}
                            selectedOptions={themeCodelist.values.filter((v) => dataset.themes.includes(v.id))}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDataset({ ...dataset, themes: v });
                            }}
                        />
                    )}
                />
            ) : null}

            {accrualPeriodicityCodelist ? (
                <FormElementGroup
                    label={t('updateFrequency')}
                    errorMessage={errors['accrualperiodicity']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={accrualPeriodicityCodelist.values}
                            selectedValue={dataset.accrualPeriodicity ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDataset({ ...dataset, accrualPeriodicity: v });
                            }}
                        />
                    )}
                />
            ) : null}

            <MultiLanguageFormGroup<string[]>
                label={t('keywords')}
                errorMessage={extractLanguageErrors(errors, 'keywords')}
                values={dataset.keywords}
                onChange={(v) => setDataset({ keywords: v })}
                emptyValue={[]}
                element={(id, value, onChange) => <MultiTextBox id={id} disabled={saving} values={value} onChange={onChange} />}
            />

            {spatialCodelist ? (
                <FormElementGroup
                    label={t('relatedSpatial')}
                    errorMessage={errors['spatial']}
                    element={(id) => (
                        <MultiSelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={spatialCodelist.values}
                            selectedOptions={spatialCodelist.values.filter((v) => dataset.spatial.includes(v.id))}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDataset({ spatial: v });
                            }}
                        />
                    )}
                />
            ) : null}

            <FormElementGroup
                label={t('timeValidityDateFrom')}
                errorMessage={errors['startdate']}
                element={(id) => <DateInput id={id} disabled={saving} value={dataset.startDate ?? ''} onDateChange={(e) => setDataset({ startDate: e })} />}
            />
            <FormElementGroup
                label={t('timeValidityDateTo')}
                errorMessage={errors['enddate']}
                element={(id) => <DateInput id={id} disabled={saving} value={dataset.endDate ?? ''} onDateChange={(e) => setDataset({ endDate: e })} />}
            />

            <MultiLanguageFormGroup<string>
                label={t('contactPointName')}
                values={dataset.contactName}
                onChange={(v) => setDataset({ contactName: v })}
                emptyValue=""
                errorMessage={extractLanguageErrors(errors, 'contactname')}
                element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
            />
            <FormElementGroup
                label={t('contactPointEmail')}
                errorMessage={errors['contactemail']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={dataset.contactEmail ?? ''} onChange={(e) => setDataset({ contactEmail: e.target.value })} />
                )}
            />

            <FormElementGroup
                label={t('landingPage')}
                errorMessage={errors['landingpage']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={dataset.landingPage ?? ''} onChange={(e) => setDataset({ landingPage: e.target.value })} />
                )}
            />
            <FormElementGroup
                label={t('specificationLink')}
                errorMessage={errors['specification']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={dataset.specification ?? ''} onChange={(e) => setDataset({ specification: e.target.value })} />
                )}
            />
            <FormElementGroup
                label={t('documentationLink')}
                errorMessage={errors['documentation']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={dataset.documentation ?? ''} onChange={(e) => setDataset({ documentation: e.target.value })} />
                )}
            />
            <FormElementGroup
                label={t('relatedSource')}
                errorMessage={errors['relation']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={dataset.relation ?? ''} onChange={(e) => setDataset({ relation: e.target.value })} />
                )}
            />

            <FormElementGroup
                label={t('euroVocClassification')}
                errorMessage={errors['eurovocthemes']}
                element={(id) => <MultiTextBox id={id} disabled={saving} values={dataset.euroVocThemes} onChange={(e) => setDataset({ euroVocThemes: e })} />}
            />

            <FormElementGroup
                label={t('spatialResolution')}
                errorMessage={errors['spatialresolutioninmeters']}
                element={(id) => (
                    <BaseInput
                        id={id}
                        disabled={saving}
                        value={dataset.spatialResolutionInMeters ?? ''}
                        onChange={(e) => setDataset({ spatialResolutionInMeters: e.target.value })}
                    />
                )}
            />
            <FormElementGroup
                label={t('timeResolution')}
                errorMessage={errors['temporalresolution']}
                element={(id) => (
                    <BaseInput
                        id={id}
                        disabled={saving}
                        value={dataset.temporalResolution ?? ''}
                        onChange={(e) => setDataset({ temporalResolution: e.target.value })}
                    />
                )}
            />

            <FormElementGroup
                label={t('applicableLegislation')}
                errorMessage={errors['applicablelegislation']}
                element={(id) => (
                    <MultiTextBox
                        id={id}
                        disabled={saving}
                        values={dataset.applicableLegislations}
                        onChange={(e) => setDataset({ applicableLegislations: e })}
                    />
                )}
            />

            {isHvd && hvdCategoryCodelist ? (
                <FormElementGroup
                    label={t('hvdCategory')}
                    errorMessage={errors['hvdcategory']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={[{ id: '', label: t('none') }, ...hvdCategoryCodelist.values]}
                            selectedValue={dataset.hvdCategory ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setDataset({ ...dataset, hvdCategory: v });
                            }}
                        />
                    )}
                />
            ) : null}

            <div style={{ marginBottom: '2em' }}>
                <Checkbox label={t('datasetIsSerie')} checked={dataset.isSerie} onCheckedChange={(v) => setDataset({ isSerie: v })} />
            </div>

            {isEnabledPartOf ? (
                <div style={{ marginBottom: '2em' }}>
                    <Checkbox
                        data-testid="is-part-of-serie"
                        label={t('datasetIsPartOfSerie')}
                        checked={dataset.isPartOf != null}
                        onCheckedChange={(v) => setDataset({ isPartOf: v ? datasets.items[0].id ?? null : null })}
                    />
                </div>
            ) : null}

            {dataset.isPartOf != null && datasets ? (
                <FormElementGroup
                    label={t('parentDataset')}
                    errorMessage={errors['ispartof']}
                    element={(id) => (
                        <SelectElementItems<Dataset | null>
                            id={id}
                            disabled={saving}
                            options={datasets.items.filter((d) => d.distributions.length === 0)}
                            selectedValue={dataset.isPartOf ?? ''}
                            renderOption={(v) => v?.name}
                            getValue={(v) => v?.id ?? ''}
                            onChange={(v) => {
                                setDataset({ isPartOf: v });
                            }}
                        />
                    )}
                />
            ) : null}
        </>
    );
}
