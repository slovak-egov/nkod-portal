import FormElementGroup from "./FormElementGroup"
import MultiLanguageFormGroup from "./MultiLanguageFormGroup"
import MultiRadio from "./MultiRadio"
import { LocalCatalogInput, extractLanguageErrors, supportedLanguages } from "../client"
import BaseInput from "./BaseInput"
import TextArea from "./TextArea"
import { useTranslation } from "react-i18next"

type Props = {
    catalog: LocalCatalogInput;
    setCatalog: (properties: Partial<LocalCatalogInput>) => void;
    errors: {[id: string]: string};
    saving: boolean;
}

type PublicOption = {
    name: string;
    value: boolean;
}
export function LocalCatalogForm(props: Props)
{

    const { catalog, setCatalog, errors } = props;
    const {t} = useTranslation();
    const saving = props.saving;

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
    

    return <>
        <MultiRadio<PublicOption> label={t('localCatalogState')} 
                                  disabled={saving}
                                    inline 
                                    options={publicOptions} 
                                    id="public-selection" 
                                    getValue={v => v.name} 
                                    renderOption={v => v.name} 
                                    selectedOption={publicOptions.find(o => o.value === catalog.isPublic) ?? publicOptions[0]} 
                                    onChange={o => setCatalog({isPublic: o.value})}  />

        <MultiLanguageFormGroup label={t('catalogName')} errorMessage={extractLanguageErrors(errors, 'name')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} disabled={saving} value={catalog.name[lang.id] ?? ''} onChange={e => setCatalog({name: {...catalog.name, [lang.id]: e.target.value}})} />} />
        <MultiLanguageFormGroup label={t('description')} errorMessage={extractLanguageErrors(errors, 'description')} languages={supportedLanguages} element={(id, lang) => <TextArea id={id} disabled={saving} value={catalog.description[lang.id] ?? ''} onChange={e => setCatalog({description: {...catalog.description, [lang.id]: e.target.value}})} />} />

        <MultiLanguageFormGroup label={t('contactPointName')} errorMessage={extractLanguageErrors(errors, 'contactname')} languages={supportedLanguages} element={(id, lang) => <BaseInput id={id} disabled={saving} value={catalog.contactName[lang.id] ?? ''} onChange={e => setCatalog({contactName: {...catalog.contactName, [lang.id]: e.target.value}})} />} />
        <FormElementGroup label={t('contactPointEmail')} errorMessage={errors['contactemail']} element={id => <BaseInput id={id} disabled={saving} value={catalog.contactEmail ?? ''} onChange={e => setCatalog({contactEmail: e.target.value})} />} />

        <FormElementGroup label={t('catalogHomePage')} errorMessage={errors['homepage']} element={id => <BaseInput id={id} disabled={saving} value={catalog.homePage ?? ''} onChange={e => setCatalog({homePage: e.target.value})} />} />
                </>
}