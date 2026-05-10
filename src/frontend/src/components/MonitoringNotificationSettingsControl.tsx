import { MonitoringSettings } from '../client';
import Checkbox from './Checkbox';
import FormElementGroup from './FormElementGroup';
import InsetText from './InsetText';
import TextArea from './TextArea';

type Props = {
    settings: MonitoringSettings | null | undefined;
    setSettings: (settings: MonitoringSettings) => void;
    disabled?: boolean;
    errors: { [id: string]: string };
};

export default function MonitoringNotificationSettingsControl(props: Props) {
    const { settings, setSettings, errors, disabled } = props;
    return (
        <>
            <p className="govuk-body">Notifikuj ma e-mailom</p>
            <InsetText>
                <p className="govuk-body">ak nastane problém pri harvestácii</p>
                <div style={{ marginBottom: '1em' }}>
                    <Checkbox
                        label="je lokálny katalóg nedostupný"
                        checked={settings?.notifyIfLocalCatalogIsUnavaiable ?? false}
                        onCheckedChange={(v) => setSettings({ ...settings, notifyIfLocalCatalogIsUnavaiable: v })}
                    />
                </div>
                <div style={{ marginBottom: '1em' }}>
                    <Checkbox
                        label="harvestácia neprebehla"
                        checked={settings?.harvestationFailed ?? false}
                        onCheckedChange={(v) => setSettings({ ...settings, harvestationFailed: v })}
                    />
                </div>
                <div style={{ marginBottom: '1em' }}>
                    <Checkbox
                        label="je úspešnosť získavania zdrojov menej ako 90%"
                        checked={settings?.successRatioIsLow ?? false}
                        onCheckedChange={(v) => setSettings({ ...settings, successRatioIsLow: v })}
                    />
                </div>
                <div style={{ marginBottom: '1em' }}>
                    <Checkbox
                        label="boli objavené problémy"
                        checked={settings?.problemsDetected ?? false}
                        onCheckedChange={(v) => setSettings({ ...settings, problemsDetected: v })}
                    />
                </div>
            </InsetText>
            <div style={{ marginBottom: '1em' }}>
                <Checkbox label="po každej harvestácii" checked={settings?.always ?? false} onCheckedChange={(v) => setSettings({ ...settings, always: v })} />
            </div>
            <div>
                <FormElementGroup
                    label="Emailové adresy, každá na nový riadok"
                    errorMessage={errors['emailaddresses']}
                    element={(id) => (
                        <TextArea
                            id={id}
                            rows={4}
                            disabled={disabled}
                            value={settings?.emailAddresses ?? ''}
                            onChange={(e) => setSettings({ ...settings, emailAddresses: e.target.value })}
                        />
                    )}
                />
            </div>
        </>
    );
}
