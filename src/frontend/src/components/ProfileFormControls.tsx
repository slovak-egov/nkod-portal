import FormElementGroup from './FormElementGroup';
import { CodelistValue, PublisherInput, knownCodelists, useCodelists } from '../client';
import BaseInput from './BaseInput';
import { useTranslation } from 'react-i18next';
import Loading from './Loading';
import ErrorAlert from './ErrorAlert';
import SelectElementItems from './SelectElementItems';

type Props = {
    publisher: PublisherInput;
    setPublisher: (properties: Partial<PublisherInput>) => void;
    errors: { [id: string]: string };
    saving: boolean;
};

const requiredCodelists = [knownCodelists.publisher.legalForm];

export function ProfileFormControls(props: Props) {
    const { publisher, setPublisher, errors } = props;
    const { t } = useTranslation();
    const saving = props.saving;
    const [codelists, loadingCodelists, errorCodelists] = useCodelists(requiredCodelists);

    const loading = loadingCodelists;
    const error = errorCodelists;
    const legalFormCodelist = codelists.find((c) => c.id === knownCodelists.publisher.legalForm);

    return (
        <>
            {loading ? <Loading /> : null}
            {error ? <ErrorAlert error={error} /> : null}

            <FormElementGroup
                label={t('websiteAddress')}
                errorMessage={errors['homePage']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={publisher.website ?? ''} onChange={(e) => setPublisher({ website: e.target.value })} />
                )}
            />
            <FormElementGroup
                label={t('contactEmailAddress')}
                errorMessage={errors['email']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={publisher.email ?? ''} onChange={(e) => setPublisher({ email: e.target.value })} />
                )}
            />
            <FormElementGroup
                label={t('contactPhoneNumber')}
                errorMessage={errors['phone']}
                element={(id) => (
                    <BaseInput id={id} disabled={saving} value={publisher.phone ?? ''} onChange={(e) => setPublisher({ phone: e.target.value })} />
                )}
            />

            {legalFormCodelist ? (
                <FormElementGroup
                    label={t('legalForm')}
                    errorMessage={errors['legalform']}
                    element={(id) => (
                        <SelectElementItems<CodelistValue>
                            id={id}
                            disabled={saving}
                            options={legalFormCodelist.values}
                            selectedValue={publisher.legalForm ?? ''}
                            renderOption={(v) => v.label}
                            getValue={(v) => v.id}
                            onChange={(v) => {
                                setPublisher({ legalForm: v });
                            }}
                        />
                    )}
                />
            ) : null}
        </>
    );
}
