import { useEffect, useState } from "react"
import FormElementGroup from "./FormElementGroup"
import MultiLanguageFormGroup from "./MultiLanguageFormGroup"
import MultiRadio from "./MultiRadio"
import { CodelistValue, Dataset, DatasetInput, UserInfo, extractLanguageErrors, knownCodelists, supportedLanguages, useCodelists, useDatasets } from "../client"
import BaseInput from "./BaseInput"
import TextArea from "./TextArea"
import MultiSelectElementItems from "./MultiSelecteElementItems"
import SelectElementItems from "./SelectElementItems"
import CodelistMultiTextBoxAutocomplete from "./CodelistMultiTextBoxAutocomplete"
import MultiTextBox from "./MultiTextBox"
import MultiCheckbox from "./MultiCheckbox"
import ErrorAlert from "./ErrorAlert"
import Loading from "./Loading"
import { useTranslation } from "react-i18next"

type Props = {
    dataset: DatasetInput;
    setDataset: (properties: Partial<DatasetInput>) => void;
    saving: boolean;
    userInfo: UserInfo|null;
    errors: {[id: string]: string}
}

type PublicOption = {
    name: string;
    value: boolean;
}

const requiredCodelists = [knownCodelists.dataset.theme, knownCodelists.dataset.type, knownCodelists.dataset.accrualPeriodicity];

type SerieSetting = {
    name: string;
    id: string;
    enableDatasetSelection: boolean;
}

export function DatasetForm(props: Props)
{
    const {t} = useTranslation();
    const serieSettings: SerieSetting[] = [
        {
            'name': t('singleDataset'),
            'id': 'single',
            'enableDatasetSelection': false
        },
        {
            'name': t('datasetIsSerie'),
            'id': 'serie',
            'enableDatasetSelection': false
        },
        {
            'name': t('datasetIsPartOfSerie'),
            'id': 'isPartOf',
            'enableDatasetSelection': true
        }
    ];

    const [isPartOf, setIsPartOf] = useState<SerieSetting>(serieSettings[0]);
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
                },
                page: 1
            });
        }
    }, [userInfo, setDatasetQueryParameters]);

    const loading = loadingDatasets || loadingCodelists;
    const error = errorDatasets ?? errorCodelists;

    const typeCodelist = codelists.find(c => c.id === knownCodelists.dataset.type);
    const themeCodelist = codelists.find(c => c.id === knownCodelists.dataset.theme);
    const accrualPeriodicityCodelist = codelists.find(c => c.id === knownCodelists.dataset.accrualPeriodicity);

    const saving = props.saving;

    return <>
    
        {loading ? <Loading /> : null}
        {error ? <ErrorAlert error={error} /> : null}

        <MultiRadio<PublicOption> label={t('datasetState')}
                                    inline 
                                    options={publicOptions} 
                                    id="public-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={publicOptions.find(o => o.value === dataset.isPublic) ?? publicOptions[0]} 
                                    onChange={o => setDataset({...dataset, isPublic: o.value})}  />

        {typeCodelist ? <FormElementGroup label={t('datasetType')} element={id => <MultiCheckbox<CodelistValue> 
            id={id} 
            options={typeCodelist.values} 
            selectedValues={dataset.type} 
            getLabel={v => v.label} 
            getValue={v => v.id} 
            onCheckedChanged={v => {setDataset({...dataset, type: v})
        }} />} /> : null}

        <MultiLanguageFormGroup label={t('datasetName')} errorMessage={extractLanguageErrors(errors, 'name')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} disabled={saving} value={dataset.name[lang.id] ?? ''} onChange={e => setDataset({name: {...dataset.name, [lang.id]: e.target.value}})} />} />
        <MultiLanguageFormGroup label={t('description')} errorMessage={extractLanguageErrors(errors, 'description')} languages={supportedLanguages} element={(id, lang) => <TextArea id={id} disabled={saving} value={dataset.description[lang.id] ?? ''} onChange={e => setDataset({description: {...dataset.description, [lang.id]: e.target.value}})} />} />

        {themeCodelist ? <FormElementGroup label={t('theme')} errorMessage={errors['themes']} element={id => <MultiSelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={themeCodelist.values} 
            selectedOptions={themeCodelist.values.filter(v => dataset.themes.includes(v.id))} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDataset({...dataset, themes: v}) }} />} /> : null}

        {accrualPeriodicityCodelist ? <FormElementGroup label={t('updateFrequency')} errorMessage={errors['accrualperiodicity']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            disabled={saving}
            options={accrualPeriodicityCodelist.values} 
            selectedValue={dataset.accrualPeriodicity ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDataset({...dataset, accrualPeriodicity: v}) }} />} /> : null}

        <MultiLanguageFormGroup label={t('keywords')} errorMessage={extractLanguageErrors(errors, 'keywords')} languages={supportedLanguages} element={(id, lang) => <MultiTextBox id={id} values={dataset.keywords[lang.id] ?? ''} onChange={e => setDataset({keywords: {...dataset.keywords, [lang.id]: e}})} />} />

        <FormElementGroup label={t('relatedSpatial')} errorMessage={errors['spatial']} element={id => <CodelistMultiTextBoxAutocomplete 
            id={id} 
            disabled={saving}
            codelistId={knownCodelists.dataset.spatial}
            selectedValues={dataset.spatial}
            onChange={v => {setDataset({spatial: v}) }} />} />
        
        <FormElementGroup label={t('timeValidityDateFrom')} errorMessage={errors['startdate']} element={id => <BaseInput id={id} disabled={saving} value={dataset.startDate ?? ''} onChange={e => setDataset({startDate: e.target.value})} />} />
        <FormElementGroup label={t('timeValidityDateTo')} errorMessage={errors['enddate']} element={id => <BaseInput id={id} disabled={saving} value={dataset.endDate ?? ''} onChange={e => setDataset({endDate: e.target.value})} />} />

        <MultiLanguageFormGroup label={t('contactPointName')} errorMessage={extractLanguageErrors(errors, 'contactname')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} disabled={saving} value={dataset.contactName[lang.id] ?? ''} onChange={e => setDataset({contactName: {...dataset.contactName, [lang.id]: e.target.value}})} />} />
        <FormElementGroup label={t('contactPointEmail')} errorMessage={errors['contactemail']} element={id => <BaseInput id={id} disabled={saving} value={dataset.contactEmail ?? ''} onChange={e => setDataset({contactEmail: e.target.value})} />} />

        <FormElementGroup label={t('documenationLink')} errorMessage={errors['documentation']} element={id => <BaseInput id={id} disabled={saving} value={dataset.documentation ?? ''} onChange={e => setDataset({documentation: e.target.value})} />} />
        <FormElementGroup label={t('specificationLink')} errorMessage={errors['specification']} element={id => <BaseInput id={id} disabled={saving} value={dataset.specification ?? ''} onChange={e => setDataset({specification: e.target.value})} />} />

        <FormElementGroup label={t('euroVocClassification')} errorMessage={errors['eurovocthemes']} element={id => <CodelistMultiTextBoxAutocomplete 
            id={id} 
            disabled={saving}
            codelistId={knownCodelists.dataset.euroVoc}
            selectedValues={dataset.euroVocThemes}
            onChange={v => {setDataset({euroVocThemes: v}) }} />} />

        <FormElementGroup label={t('spatialResolution')} errorMessage={errors['spatialresolutioninmeters']} element={id => <BaseInput id={id} disabled={saving} value={dataset.spatialResolutionInMeters ?? ''} onChange={e => setDataset({spatialResolutionInMeters: Number(e.target.value)})} />} />
        <FormElementGroup label={t('timeResolution')} errorMessage={errors['temporalresolution']} element={id => <BaseInput id={id} disabled={saving} value={dataset.temporalResolution ?? ''} onChange={e => setDataset({temporalResolution: e.target.value})} />} />

        <MultiRadio<SerieSetting> label={t('datasetData')} disabled={saving} options={serieSettings} onChange={setIsPartOf} selectedOption={isPartOf} id="serie-settings" getValue={v => v.id} renderOption={v => v.name} />

        {isPartOf.enableDatasetSelection && datasets ? <FormElementGroup label="Nadradený dataset" errorMessage={errors['ispartof']} element={id => <SelectElementItems<Dataset|null> 
                id={id} 
                disabled={saving}
                options={datasets.items.filter(d => d.distributions.length === 0)} 
                selectedValue={dataset.isPartOf ?? ''} 
                renderOption={v => v?.name} 
                getValue={v => v?.id ?? ''} 
                onChange={v => {setDataset({isPartOf: v}) }} />} /> : null}
                </>
}