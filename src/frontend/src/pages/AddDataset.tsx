import { supportedLanguages, useDatasetAdd } from "../client";

import PageHeader from "../components/PageHeader";
import MultiCheckbox from "../components/MultiCheckbox";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import MultiRadio from "../components/MultiRadio";
import BaseInput from "../components/BaseInput";
import MultiLanguageFormGroup from "../components/MultiLanguageFormGroup";
import Button from "../components/Button";

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

export default function AddDataset()
{
    const [dataset, setDataset, errors, saving, saveResult, save] = useDatasetAdd({
        id: '',
        isPublic: false,
        name: {'sk': ''}
    });

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam datasetov', link: '/'}, {title: 'Nový dataset'}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>Nový dataset</PageHeader>
                    <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                    </p>

                    <MultiRadio<PublicOption> label="Stav datasetu" 
                                              inline 
                                              options={publicOptions} 
                                              id="public-selection" 
                                              getValue={v => v.name} 
                                              renderOption={v => v.name} 
                                              selectedOption={publicOptions.find(o => o.value === dataset.isPublic) ?? publicOptions[0]} 
                                              onChange={o => setDataset({...dataset, isPublic: o.value})}  />

                    <FormElementGroup label="Typ datasetu" element={id => <MultiCheckbox items={['Publikačné minimum OVM', 'Vysoká hodnota dát', 'Najžiadanejší dataset']} />} />

                    <FormElementGroup label="Typ HVD" element={id => <BaseInput id={id} />} />

                    <MultiLanguageFormGroup label="Názov datasetu" languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} value={dataset.name['sk'] ?? ''} onChange={e => setDataset({name: {...dataset.name, [lang.id]: e.target.value}})} />} />
                    
                    {/* <MultiLanguageFormGroup label="Popis" element={id => <TextArea id={id} />} />
                    <MultiFormGroup label="Kľúčové slová" element={id => <BaseInput id={id} />} />
                    <MultiFormGroup label="Téma datasetu (základná)" element={id => <BaseInput id={id} />} />
                    <MultiFormGroup label="EuroVoc témy" element={id => <BaseInput id={id} />} />
                    <MultiFormGroup label="Doplnkové témy" element={id => <BaseInput id={id} />} />
                    <MultiFormGroup label="URL na špecifikácie datasetu (Otvorené formálne normy)" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="URL na dokumentáciu datasetu" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Periodicita publikácie" element={id => <SelectElement id={id}><option value={0}>mesačne</option></SelectElement>} />
                    <FormElementGroup label="Územná platnosť" element={id => <SelectElement id={id}><option value={0}>Slovenská republika</option></SelectElement>} />
                    <FormElementGroup label="Časová platnosť od" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Časová platnosť do" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Časové rozlíšenie datasetu" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Priestorové rozlíšenie datasetu (v metroch)" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="Meno a priezvisko dátového kurátora" element={id => <BaseInput id={id} />} />
                    <FormElementGroup label="E-mail dátového kurátora" element={id => <BaseInput id={id} />} /> */}
                    {/* <MultiRadio label="Údaje datasetu" items={['Samostatný dataset', 'Dataset je séria', 'Dataset patrí do série']} /> */}

                    <Button style={{marginRight: '20px'}} onClick={save} disabled={saving}>
                        Uložiť dataset
                    </Button>
                    
                    <Button>
                        Uložiť dataset a pridať distribúciu
                    </Button>
                </div>
            </MainContent>
        </>;
}