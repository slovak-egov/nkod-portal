import FormElementGroup from "./FormElementGroup"
import MultiLanguageFormGroup from "./MultiLanguageFormGroup"
import MultiRadio from "./MultiRadio"
import { LocalCatalogInput, UserInfo, extractLanguageErrors, supportedLanguages, useCodelists } from "../client"
import BaseInput from "./BaseInput"
import TextArea from "./TextArea"

type Props = {
    catalog: LocalCatalogInput;
    setCatalog: (properties: Partial<LocalCatalogInput>) => void;
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

const requiredCodelists: string[] = [];

export function LocalCatalogForm(props: Props)
{
    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);

    const { catalog, setCatalog, userInfo, errors } = props;

    return <><MultiRadio<PublicOption> label="Stav lokálneho katalógu" 
                                    inline 
                                    options={publicOptions} 
                                    id="public-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={publicOptions.find(o => o.value === catalog.isPublic) ?? publicOptions[0]} 
                                    onChange={o => setCatalog({isPublic: o.value})}  />

        <MultiLanguageFormGroup label="Názov katalógu" errorMessage={extractLanguageErrors(errors, 'name')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} value={catalog.name[lang.id] ?? ''} onChange={e => setCatalog({name: {...catalog.name, [lang.id]: e.target.value}})} />} />
        <MultiLanguageFormGroup label="Popis" errorMessage={extractLanguageErrors(errors, 'description')} languages={supportedLanguages} element={(id, lang) => <TextArea id={id} value={catalog.description[lang.id] ?? ''} onChange={e => setCatalog({description: {...catalog.description, [lang.id]: e.target.value}})} />} />

        <MultiLanguageFormGroup label="Kontaktný bod, meno" errorMessage={extractLanguageErrors(errors, 'contactname')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} value={catalog.contactName[lang.id] ?? ''} onChange={e => setCatalog({contactName: {...catalog.contactName, [lang.id]: e.target.value}})} />} />
        <FormElementGroup label="Kontaktný bod, e-mailová adresa" errorMessage={errors['contactemail']} element={id => <BaseInput id={id} value={catalog.contactEmail ?? ''} onChange={e => setCatalog({contactEmail: e.target.value})} />} />

        <FormElementGroup label="Domáca stránka katalógu" errorMessage={errors['homepage']} element={id => <BaseInput id={id} value={catalog.homePage ?? ''} onChange={e => setCatalog({homePage: e.target.value})} />} />
                </>
}