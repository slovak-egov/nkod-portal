import FormElementGroup from './FormElementGroup';
import { AdminPublisherInput, extractLanguageErrors } from '../client';
import BaseInput from './BaseInput';
import { useTranslation } from 'react-i18next';
import MultiLanguageFormGroup from './MultiLanguageFormGroup';
import { ProfileFormControls } from './ProfileFormControls';
import MultiRadio from './MultiRadio';

type Props = {
    publisher: AdminPublisherInput;
    setPublisher: (properties: Partial<AdminPublisherInput>) => void;
    errors: { [id: string]: string };
    saving: boolean;
};

type PublicOption = {
    name: string;
    value: boolean;
};


export function AdminPublisherForm(props: Props) {
    const { publisher, setPublisher, errors } = props;
    const { t } = useTranslation();
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

    return (
        <>
            <MultiRadio<PublicOption>
                label={t('state')}
                inline
                options={publicOptions}
                id="public-selection"
                getValue={(v) => v.name}
                renderOption={(v) => v.name}
                selectedOption={publicOptions.find((o) => o.value === publisher.isEnabled) ?? publicOptions[0]}
                onChange={(o) => setPublisher({ isEnabled: o.value })}
            />

            <MultiLanguageFormGroup<string>
                label={t('name')}
                values={publisher.name}
                onChange={(v) => setPublisher({ name: v })}
                emptyValue=""
                errorMessage={extractLanguageErrors(errors, 'name')}
                element={(id, value, onChange) => <BaseInput id={id} disabled={saving} value={value} onChange={(e) => onChange(e.target.value)} />}
            />

            <FormElementGroup
                label="URI"
                errorMessage={errors['uri']}
                element={(id) => <BaseInput id={id} disabled={saving} value={publisher.uri ?? ''} onChange={(e) => setPublisher({ uri: e.target.value })} />}
            />

            <ProfileFormControls publisher={publisher} setPublisher={setPublisher} errors={errors} saving={saving} />
        </>
    );
}
