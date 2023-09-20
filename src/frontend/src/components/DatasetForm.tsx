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

type Props = {
    dataset: DatasetInput;
    setDataset: (properties: Partial<DatasetInput>) => void;
    userInfo: UserInfo|null;
    errors: {[id: string]: string}
}

type PublicOption = {
    name: string;
    value: boolean;
}

const publicOptions = [
    {
        name: 'Zverejnený',
        value: true
    },
    {
        name: 'Nezverejnený',
        value: false
    }
];

const requiredCodelists = [knownCodelists.dataset.theme, knownCodelists.dataset.type, knownCodelists.dataset.accrualPeriodicity];

type SerieSetting = {
    name: string;
    id: string;
    enableDatasetSelection: boolean;
}

const serieSettings: SerieSetting[] = [
    {
        'name': 'Samostatný dataset',
        'id': 'single',
        'enableDatasetSelection': false
    },
    {
        'name': 'Dataset je séria',
        'id': 'serie',
        'enableDatasetSelection': false
    },
    {
        'name': 'Dataset patrí do série',
        'id': 'isPartOf',
        'enableDatasetSelection': true
    }
];

export function DatasetForm(props: Props)
{
    const [isPartOf, setIsPartOf] = useState<SerieSetting>(serieSettings[0]);
    const [datasets, datasetsQuery, setDatasetQueryParameters, loadingDatasets, errorDatasets] = useDatasets({
        pageSize: 100,
        page: 0
    });

    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);

    const { dataset, setDataset, userInfo, errors } = props;

    useEffect(() => {
        if (userInfo?.publisher) {
            setDatasetQueryParameters({
                filters: {
                    publishers: [userInfo.publisher],
                },
                page: 1
            });
        }
    }, [userInfo]);

    const typeCodelist = codelists.find(c => c.id === knownCodelists.dataset.type);
    const themeCodelist = codelists.find(c => c.id === knownCodelists.dataset.theme);
    const accrualPeriodicityCodelist = codelists.find(c => c.id === knownCodelists.dataset.accrualPeriodicity);

    return <><MultiRadio<PublicOption> label="Stav datasetu" 
                                    inline 
                                    options={publicOptions} 
                                    id="public-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={publicOptions.find(o => o.value === dataset.isPublic) ?? publicOptions[0]} 
                                    onChange={o => setDataset({...dataset, isPublic: o.value})}  />

        {typeCodelist ? <FormElementGroup label="Typ datasetu" element={id => <MultiCheckbox<CodelistValue> 
            id={id} 
            options={typeCodelist.values} 
            selectedValues={dataset.type} 
            getLabel={v => v.label} 
            getValue={v => v.id} 
            onCheckedChanged={v => {setDataset({...dataset, type: v})
        }} />} /> : null}

        <MultiLanguageFormGroup label="Názov datasetu" errorMessage={extractLanguageErrors(errors, 'name')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} value={dataset.name[lang.id] ?? ''} onChange={e => setDataset({name: {...dataset.name, [lang.id]: e.target.value}})} />} />
        <MultiLanguageFormGroup label="Popis" errorMessage={extractLanguageErrors(errors, 'description')} languages={supportedLanguages} element={(id, lang) => <TextArea id={id} value={dataset.description[lang.id] ?? ''} onChange={e => setDataset({description: {...dataset.description, [lang.id]: e.target.value}})} />} />

        {themeCodelist ? <FormElementGroup label="Téma" errorMessage={errors['themes']} element={id => <MultiSelectElementItems<CodelistValue> 
            id={id} 
            options={themeCodelist.values} 
            selectedOptions={themeCodelist.values.filter(v => dataset.themes.includes(v.id))} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDataset({...dataset, themes: v}) }} />} /> : null}

        {accrualPeriodicityCodelist ? <FormElementGroup label="Periodicita aktualizácie" errorMessage={errors['accrualperiodicity']} element={id => <SelectElementItems<CodelistValue> 
            id={id} 
            options={accrualPeriodicityCodelist.values} 
            selectedValue={dataset.accrualPeriodicity ?? ''} 
            renderOption={v => v.label} 
            getValue={v => v.id} 
            onChange={v => {setDataset({...dataset, accrualPeriodicity: v}) }} />} /> : null}

        <MultiLanguageFormGroup label="Kľúčové slová" errorMessage={extractLanguageErrors(errors, 'keywords')} languages={supportedLanguages} element={(id, lang) => <MultiTextBox id={id} values={dataset.keywords[lang.id] ?? ''} onChange={e => setDataset({keywords: {...dataset.keywords, [lang.id]: e}})} />} />

        <FormElementGroup label="Súvisiace geografické územie" errorMessage={errors['spatial']} element={id => <CodelistMultiTextBoxAutocomplete 
            id={id} 
            codelistId={knownCodelists.dataset.spatial}
            selectedValues={dataset.spatial}
            onChange={v => {setDataset({spatial: v}) }} />} />
        
        <FormElementGroup label="Časové pokrytie, dátum od" errorMessage={errors['startdate']} element={id => <BaseInput id={id} value={dataset.startDate ?? ''} onChange={e => setDataset({startDate: e.target.value})} />} />
        <FormElementGroup label="Časové pokrytie, dátum do" errorMessage={errors['enddate']} element={id => <BaseInput id={id} value={dataset.endDate ?? ''} onChange={e => setDataset({endDate: e.target.value})} />} />

        <MultiLanguageFormGroup label="Kontaktný bod, meno" errorMessage={extractLanguageErrors(errors, 'contactname')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} value={dataset.contactName[lang.id] ?? ''} onChange={e => setDataset({contactName: {...dataset.contactName, [lang.id]: e.target.value}})} />} />
        <FormElementGroup label="Kontaktný bod, e-mailová adresa" errorMessage={errors['contactemail']} element={id => <BaseInput id={id} value={dataset.contactEmail ?? ''} onChange={e => setDataset({contactEmail: e.target.value})} />} />

        <FormElementGroup label="Odkaz na dokumentáciu" errorMessage={errors['documentation']} element={id => <BaseInput id={id} value={dataset.documentation ?? ''} onChange={e => setDataset({documentation: e.target.value})} />} />
        <FormElementGroup label="Odkaz na špecifikáciu" errorMessage={errors['specification']} element={id => <BaseInput id={id} value={dataset.specification ?? ''} onChange={e => setDataset({specification: e.target.value})} />} />

        <FormElementGroup label="Klasifikácia podľa EuroVoc" errorMessage={errors['eurovocthemes']} element={id => <CodelistMultiTextBoxAutocomplete 
            id={id} 
            codelistId={knownCodelists.dataset.euroVoc}
            selectedValues={dataset.euroVocThemes}
            onChange={v => {setDataset({euroVocThemes: v}) }} />} />

        <FormElementGroup label="Priestorové rozlíšenie v metroch" errorMessage={errors['spatialresolutioninmeters']} element={id => <BaseInput id={id} value={dataset.spatialResolutionInMeters ?? ''} onChange={e => setDataset({spatialResolutionInMeters: Number(e.target.value)})} />} />
        <FormElementGroup label="Časové rozlíšenie" errorMessage={errors['temporalresolution']} element={id => <BaseInput id={id} value={dataset.temporalResolution ?? ''} onChange={e => setDataset({temporalResolution: e.target.value})} />} />

        <MultiRadio<SerieSetting> label="Údaje datasetu" options={serieSettings} onChange={setIsPartOf} selectedOption={isPartOf} id="serie-settings" getValue={v => v.id} renderOption={v => v.name} />

        {isPartOf.enableDatasetSelection && datasets ? <FormElementGroup label="Nadradený dataset" errorMessage={errors['ispartof']} element={id => <SelectElementItems<Dataset|null> 
                id={id} 
                options={datasets.items.filter(d => d.distributions.length === 0)} 
                selectedValue={dataset.isPartOf ?? ''} 
                renderOption={v => v?.name} 
                getValue={v => v?.id ?? ''} 
                onChange={v => {setDataset({isPartOf: v}) }} />} /> : null}
                </>
}